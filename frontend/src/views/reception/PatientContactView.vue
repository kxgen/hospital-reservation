<script setup>
import { ref, onMounted, computed } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'

const auth = useAuthStore()
const alertStore = useAlertStore()

const activeTab = ref('priority') // 'registry', 'priority', 'compose'
const patients = ref([])
const escalations = ref([])
const isLoading = ref(false)
const searchQuery = ref('')
const selectedPatient = ref(null)

const messageTitle = ref('')
const messageText = ref('')
const messageType = ref('Administrative') // Receptionists default to Administrative/Staff type
const isSending = ref(false)

const getAuthHeaders = () => ({
  headers: { Authorization: `Bearer ${auth.token}` }
})

const fetchPatients = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/patient`, getAuthHeaders())
    patients.value = res.data || []
  } catch (err) {
    console.error('Failed to load patients', err)
  } finally {
    isLoading.value = false
  }
}

const fetchEscalations = async () => {
  isLoading.value = true
  try {
    const res = await axios.get(`/api/receptionist/appointments/escalations`, getAuthHeaders())
    escalations.value = res.data || []
  } catch (err) {
    console.error('Failed to load escalations', err)
  } finally {
    isLoading.value = false
  }
}

const filteredPatients = computed(() => {
  if (!searchQuery.value) return patients.value
  const q = searchQuery.value.toLowerCase()
  return patients.value.filter(p => 
    `${p.firstName} ${p.lastName}`.toLowerCase().includes(q) || 
    p.phone?.includes(q)
  )
})

const selectPatient = (p) => {
  selectedPatient.value = p
  messageTitle.value = ''
  messageText.value = ''
  activeTab.value = 'compose'
}

const sendNotification = async () => {
  if (!selectedPatient.value || !messageText.value || !messageTitle.value) {
    alertStore.showAlert('Please complete all fields.', 'warning')
    return
  }

  isSending.value = true
  try {
    await axios.post(`/api/notifications/send`, {
      patientId: selectedPatient.value.patientId || selectedPatient.value.patientDbId,
      title: messageTitle.value,
      message: messageText.value,
      type: messageType.value
    }, getAuthHeaders())
    alertStore.showAlert('Notification sent successfully!', 'success')
    messageText.value = ''
    messageTitle.value = ''
  } catch (err) {
    console.error('Failed to send notification', err)
    alertStore.showAlert('Failed to send notification.', 'error')
  } finally {
    isSending.value = false
  }
}

const handleCancel = async (id) => {
    if (!confirm('Are you sure you want to cancel this unconfirmed appointment?')) return
    try {
        await axios.put(`/api/appointments/${id}/cancel`, {}, getAuthHeaders())
        alertStore.showAlert('Appointment cancelled successfully.', 'success')
        fetchEscalations()
    } catch (err) {
        console.error('Failed to cancel', err)
    }
}

const handleReschedule = (esc) => {
    // Escalate might not have accountId info directly if mapped from appointment, 
    // but the selectPatient logic will handle the UI state.
    selectPatient({
        patientId: esc.patientDbId,
        firstName: esc.patientName.split(' ')[0],
        lastName: esc.patientName.split(' ')[1] || '',
        phone: esc.patientPhone,
        accountId: esc.accountId // Assuming we add this to escalation resp if needed, or we search p in registry
    })
    
    // Find patient in registry to see if they have an account
    const registryPatient = patients.value.find(p => p.patientId === esc.patientDbId)
    if (registryPatient) {
        selectedPatient.value = registryPatient
    }

    messageTitle.value = "Urgent: Unconfirmed Appointment"
    messageText.value = `Regarding your upcoming visit with Dr. ${esc.doctorName}: Your attendance for ${formatTime(esc.startTime)} remains unconfirmed. Please confirm via portal or contact us to reschedule.`
}

const formatTime = (iso) => {
  return new Date(iso).toLocaleDateString('en-US', { 
    month: 'short', 
    day: 'numeric', 
    hour: '2-digit', 
    minute: '2-digit' 
  })
}

onMounted(() => {
    fetchPatients()
    fetchEscalations()
})
</script>

<template>
  <div class="contact-hub-page">
    <header class="page-header">
      <div class="header-content">
        <h1>Comm<span>Center</span></h1>
        <p>Staff communication hub for patient outreach and clinical coordination.</p>
      </div>
    </header>

    <div class="hub-container">
      <!-- Top Navigation Tabs -->
      <nav class="hub-tabs">
        <button 
          :class="['tab-btn', { active: activeTab === 'priority' }]"
          @click="activeTab = 'priority'"
        >
          <span class="tab-text">Inbox & Alerts</span>
          <span v-if="escalations.length > 0" class="tab-badge">{{ escalations.length }}</span>
        </button>

        <button 
          :class="['tab-btn', { active: activeTab === 'registry' }]"
          @click="activeTab = 'registry'"
        >
          <span class="tab-text">Patients List</span>
        </button>

        <button 
          :class="['tab-btn', { active: activeTab === 'compose' }]"
          @click="activeTab = 'compose'"
        >
          <span class="tab-text">Composer</span>
        </button>
      </nav>

      <!-- Content Area -->
      <main class="hub-content">
        <!-- Tab 1: Registry -->
        <section v-if="activeTab === 'registry'" class="registry-view animate-fade-in">
          <div class="section-actions">
            <input 
              v-model="searchQuery" 
              type="text" 
              placeholder="Search by patient name or phone number..." 
              class="hub-search"
            >
          </div>
          
          <div class="registry-list">
            <div v-if="isLoading" class="list-status">Loading patient registry...</div>
            <div v-else-if="filteredPatients.length === 0" class="list-status">No patients matching your search.</div>
            
            <div 
              v-for="p in filteredPatients" 
              :key="p.patientId" 
              class="patient-record-card"
              @click="selectPatient(p)"
            >
              <div class="record-main">
                  <div class="p-avatar" :class="{ 'guest-avatar': !p.accountId }">{{ p.firstName[0] }}{{ p.lastName[0] }}</div>
                  <div class="p-info">
                    <div class="name-row">
                        <span class="p-name">{{ p.firstName }} {{ p.lastName }}</span>
                        <span v-if="p.accountId" class="badge-account">Account</span>
                        <span v-else class="badge-guest">Guest</span>
                    </div>
                    <span class="p-phone">{{ p.phone || 'No phone' }}</span>
                  </div>
              </div>
              <button class="btn-select-alt">{{ p.accountId ? 'Send Message' : 'View Contact' }} </button>
            </div>
          </div>
        </section>

        <!-- Tab 2: Alerts -->
        <section v-if="activeTab === 'priority'" class="alerts-view animate-fade-in">
          <div class="alert-list">
            <div v-if="isLoading" class="list-status">Checking for staff updates...</div>
            
            <!-- Escalations section (part of general alerts) -->
            <div class="alert-group" v-if="escalations.length > 0">
                <h4 class="group-title">Critical Escalations</h4>
                <div 
                  v-for="esc in escalations" 
                  :key="esc.id" 
                  class="escalation-card-unified"
                >
                  <div class="alert-header">
                      <div class="alert-patient-info">
                          <span class="p-name-alt">{{ esc.patientName }}</span>
                          <span class="p-time-alt">{{ formatTime(esc.startTime) }}</span>
                      </div>
                      <span class="alert-tag">UNCONFIRMED</span>
                  </div>
                  <div class="alert-details">
                      <p>Clinician: <strong>Dr. {{ esc.doctorName }}</strong></p>
                      <p>Mobile: <strong>{{ esc.patientPhone }}</strong></p>
                  </div>
                  <div class="alert-footer">
                    <button class="btn-primary-small" @click="handleReschedule(esc)">Contact Patient</button>
                    <button class="btn-text-small" @click="handleCancel(esc.id)">Cancel Appointment</button>
                  </div>
                </div>
            </div>

            <!-- Empty state if nothing at all -->
            <div v-if="escalations.length === 0" class="list-status">
                <p>Your inbox is clear.</p>
            </div>
          </div>
        </section>

        <!-- Tab 3: Compose -->
        <section v-if="activeTab === 'compose'" class="compose-view animate-fade-in">
          <div v-if="!selectedPatient" class="empty-composer">
            <p>You haven't selected a patient yet.</p>
            <button @click="activeTab = 'registry'" class="btn-link">Browse Registry</button>
          </div>

          <div v-else class="center-composer">
            <header class="composer-info">
                <div class="selected-p-card">
                    <div class="p-avatar-lg" :class="{ 'guest-avatar': !selectedPatient.accountId }">{{ selectedPatient.firstName[0] }}{{ selectedPatient.lastName[0] }}</div>
                    <div class="p-meta">
                        <h2>{{ selectedPatient.firstName }} {{ selectedPatient.lastName }}</h2>
                        <p>Patient Contact: <strong>{{ selectedPatient.phone || 'N/A' }}</strong></p>
                        <span v-if="!selectedPatient.accountId" class="guest-notice-badge">Contact via Phone Only</span>
                    </div>
                </div>
                <button class="btn-change-p" @click="activeTab = 'registry'">Select Another</button>
            </header>

            <!-- Case 1: Patient has an account -->
            <div v-if="selectedPatient.accountId" class="composer-box">
                <div class="field-item">
                    <label>Hospital Staff Message</label>
                    <p class="type-clarification">This message will be sent through the secure hospital portal with an <strong>Administrative</strong> tag.</p>
                </div>

                <div class="field-item">
                    <label>Subject</label>
                    <input v-model="messageTitle" type="text" placeholder="Enter message subject...">
                </div>

                <div class="field-item">
                    <label>Content</label>
                    <textarea v-model="messageText" rows="10" placeholder="Type your message here..."></textarea>
                </div>

                <div class="composer-actions-hub">
                    <button class="btn-dispatch" :disabled="isSending" @click="sendNotification">
                        {{ isSending ? 'Sending...' : 'Send Portal Message' }}
                    </button>
                    <p class="sender-identity">Source: <strong>Hospital Staff</strong></p>
                </div>
            </div>

            <!-- Case 2: Guest Patient (No Account) -->
            <div v-else class="guest-contact-only">
                <div class="guest-warning-card">
                    <div class="warning-icon">📞</div>
                    <div class="warning-text">
                        <h3>Phone Outreach Required</h3>
                        <p>This patient is currently registered as a guest and does not have a secure portal account. Please contact them at <strong>{{ selectedPatient.phone || 'the number provided' }}</strong> to coordinate their care.</p>
                    </div>
                </div>
                <button class="btn-registry-return" @click="activeTab = 'registry'">Return to Registry</button>
            </div>
          </div>
        </section>
      </main>
    </div>
  </div>
</template>

<style scoped>
.contact-hub-page {
  max-width: 1200px;
  margin: 40px auto;
  padding: 0 40px;
  min-height: 90vh;
}

/* Header */
.page-header {
  margin-bottom: 40px;
}

.header-content h1 { font-size: 2.5rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.header-content h1 span { color: var(--color-primary); }
.header-content p { color: var(--text-secondary); font-size: 1.1rem; margin-top: 5px; }

/* Hub Layout */
.hub-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Tabs Navigation */
.hub-tabs {
  display: flex;
  justify-content: flex-start;
  gap: 12px;
  background: var(--bg-card);
  padding: 10px;
  border-radius: 0;
  border: 1px solid var(--border-default);
  box-shadow: var(--shadow-sm);
  width: fit-content;
}

.tab-btn {
  padding: 12px 25px;
  border: none;
  background: none;
  border-radius: 0;
  cursor: pointer;
  font-weight: 700;
  color: var(--text-secondary);
  transition: all 0.2s;
  display: flex;
  align-items: center;
  gap: 10px;
}

.tab-btn:hover { background: var(--bg-input); color: var(--text-primary); }
.tab-btn.active { background: var(--color-primary); color: white; }

.tab-badge {
  background: #ef4444;
  color: white;
  font-size: 0.7rem;
  font-weight: 800;
  padding: 2px 8px;
  border-radius: 50px;
}

/* Content Area */
.hub-content {
  background: var(--bg-card);
  border-radius: 0;
  border: 1px solid var(--border-default);
  padding: 40px;
  box-shadow: var(--shadow-md);
}

.list-status { text-align: center; padding: 100px 20px; color: var(--text-secondary); font-style: italic; }

/* Registry */
.hub-search {
    width: 100%;
    padding: 18px 25px;
    border-radius: 0;
    border: 1px solid var(--border-default);
    background: var(--bg-body);
    font-size: 1.05rem;
    margin-bottom: 30px;
    color: var(--text-primary);
}

.registry-list { display: flex; flex-direction: column; gap: 12px; }
.patient-record-card {
    display: flex;
    justify-content: space-between;
    align-items: center;
    padding: 20px 25px;
    background: var(--bg-card);
    border: 1px solid var(--border-default);
    border-radius: 0;
    cursor: pointer;
    transition: 0.2s;
}
.patient-record-card:hover { border-color: var(--color-primary); background: var(--color-primary-light); transform: translateY(-2px); }

.record-main { display: flex; align-items: center; gap: 20px; }
.p-avatar { width: 50px; height: 50px; border-radius: 0; background: var(--bg-input); color: var(--color-primary); display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 1.1rem; }
.p-avatar.guest-avatar { background: var(--bg-input); color: var(--text-tertiary); }
.p-info { display: flex; flex-direction: column; }
.name-row { display: flex; align-items: center; gap: 10px; }
.p-name { font-weight: 800; color: var(--text-primary); font-size: 1.1rem; }
.p-phone { font-size: 0.9rem; color: var(--text-secondary); }

.badge-account { background: #dcfce7; color: #166534; font-size: 0.65rem; padding: 2px 8px; border-radius: 50px; font-weight: 800; }
.badge-guest { background: #f3f4f6; color: #4b5563; font-size: 0.65rem; padding: 2px 8px; border-radius: 50px; font-weight: 800; }

.btn-select-alt { padding: 10px 20px; background: white; border: 2px solid var(--color-primary); color: var(--color-primary); border-radius: 0; font-weight: 800; cursor: pointer; transition: 0.2s; }
.btn-select-alt:hover { background: var(--color-primary); color: white; }

/* Alerts */
.alert-list { display: flex; flex-direction: column; gap: 30px; }
.alert-group { display: flex; flex-direction: column; gap: 15px; }
.group-title { font-size: 0.9rem; font-weight: 900; color: var(--text-tertiary); text-transform: uppercase; letter-spacing: 1px; border-bottom: 2px solid var(--border-default); padding-bottom: 10px; }
.escalation-card-unified {
    padding: 30px; border-radius: 0; border: 1px solid #fee2e2;
    background: #fffafa;
}
.alert-header { display: flex; justify-content: space-between; align-items: flex-start; margin-bottom: 15px; }
.p-name-alt { display: block; font-weight: 900; font-size: 1.2rem; color: var(--text-primary); }
.p-time-alt { display: block; color: #ef4444; font-weight: 800; font-size: 0.95rem; margin-top: 2px; }
.alert-tag { background: #fee2e2; color: #b91c1c; font-size: 0.65rem; font-weight: 900; padding: 4px 10px; border-radius: 6px; }

.alert-details { border-top: 1px dashed #fecaca; padding-top: 15px; margin-bottom: 25px; }
.alert-details p { margin: 6px 0; color: var(--text-secondary); font-size: 0.95rem; }
.alert-details strong { color: var(--text-primary); }

.alert-footer { display: flex; gap: 15px; align-items: center; }
.btn-primary-small { padding: 12px 25px; background: #ef4444; color: white; border: none; border-radius: 0; font-weight: 800; cursor: pointer; }
.btn-text-small { background: none; border: none; color: var(--text-tertiary); text-decoration: underline; font-weight: 700; cursor: pointer; font-size: 0.85rem; }

/* Composer */
.empty-composer { text-align: center; padding: 100px 20px; color: var(--text-secondary); }
.btn-link { background: none; border: none; color: var(--color-primary); font-weight: 800; text-decoration: underline; cursor: pointer; margin-top: 10px; font-size: 1rem; }

.center-composer { max-width: 700px; margin: 0 auto; }
.composer-info { display: flex; justify-content: space-between; align-items: center; margin-bottom: 35px; background: var(--bg-body); padding: 20px; border-radius: 0; border: 1px solid var(--border-default); }
.selected-p-card { display: flex; align-items: center; gap: 20px; }
.p-avatar-lg { width: 60px; height: 60px; border-radius: 0; background: var(--color-primary); color: white; display: flex; align-items: center; justify-content: center; font-weight: 800; font-size: 1.5rem; }
.p-avatar-lg.guest-avatar { background: var(--color-gray-300); }
.p-meta h2 { margin: 0; font-size: 1.3rem; }
.p-meta p { margin: 4px 0 0; font-size: 0.9rem; color: var(--text-secondary); }
.btn-change-p { background: none; border: none; color: var(--text-tertiary); font-weight: 700; cursor: pointer; text-decoration: underline; font-size: 0.85rem; }

.guest-notice-badge { display: inline-block; margin-top: 10px; background: #fef3c7; color: #92400e; font-size: 0.7rem; font-weight: 900; padding: 4px 10px; border-radius: 6px; text-transform: uppercase; }

.composer-box { display: flex; flex-direction: column; gap: 30px; }
.field-item { display: flex; flex-direction: column; gap: 12px; }
.field-item label { font-size: 0.8rem; font-weight: 800; color: var(--text-tertiary); text-transform: uppercase; letter-spacing: 1px; }

.type-clarification { font-size: 0.9rem; color: var(--text-secondary); line-height: 1.5; margin: 0; }
.type-clarification strong { color: var(--color-primary); }

.composer-box input, .composer-box textarea { padding: 18px; border-radius: 0; border: 1.5px solid var(--border-default); background: var(--bg-body); font-size: 1rem; color: var(--text-primary); }

.composer-actions-hub { display: flex; flex-direction: column; align-items: center; gap: 15px; margin-top: 10px; }
.btn-dispatch { width: 100%; padding: 18px; background: var(--color-primary); color: white; border: none; border-radius: 0; font-weight: 900; font-size: 1.1rem; cursor: pointer; box-shadow: var(--shadow-md); transition: 0.3s; }
.btn-dispatch:hover { transform: translateY(-2px); box-shadow: 0 8px 15px rgba(var(--color-primary-rgb), 0.3); }

.sender-identity { font-size: 0.9rem; color: var(--text-secondary); }

/* Guest Contact view */
.guest-contact-only { text-align: center; padding: 20px 0; }
.guest-warning-card { display: flex; align-items: center; gap: 30px; background: #fffbeb; border: 1px solid #fef3c7; padding: 40px; border-radius: 0; text-align: left; margin-bottom: 30px; }
.warning-icon { font-size: 3rem; }
.warning-text h3 { margin: 0; font-size: 1.4rem; color: #92400e; }
.warning-text p { margin: 10px 0 0; line-height: 1.6; color: #92400e; }
.btn-registry-return { padding: 15px 30px; background: var(--color-primary); color: white; border: none; border-radius: 0; font-weight: 800; cursor: pointer; }

.animate-fade-in { animation: fadeIn 0.4s ease-out; }
@keyframes fadeIn { from { opacity: 0; transform: translateY(10px); } to { opacity: 1; transform: translateY(0); } }

@media (max-width: 600px) {
    .hub-tabs { flex-direction: column; }
    .hub-content { padding: 25px 20px; }
    .patient-record-card { flex-direction: column; gap: 15px; text-align: center; }
    .record-main { flex-direction: column; }
    .btn-select-alt { width: 100%; }
}
</style>
