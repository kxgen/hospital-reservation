<template>
  <div class="provider-availability">
    <header class="page-header">
      <h1 class="page-title">Provider Availability Management</h1>
      <p class="page-subtitle">Manage clinical schedules and block unavailable times.</p>
    </header>

    <!-- Top Control Bar: Select Doctor -->
    <div class="control-bar shadow-sm">
      <div class="search-group">
        <label for="doctor-select">Find Provider:</label>
        <select id="doctor-select" v-model="form.doctorId" class="doctor-select">
          <option value="">-- Select a Provider --</option>
          <option v-for="doc in doctors" :key="doc.id" :value="doc.id">
            Dr. {{ doc.firstName }} {{ doc.lastName }} ({{ doc.specialtyName || 'General' }})
          </option>
        </select>
      </div>
    </div>

    <!-- State 1: No Selection -->
    <div v-if="!form.doctorId" class="empty-state">
      <div class="empty-icon"></div>
      <h3>No Provider Selected</h3>
      <p>Please select a clinical provider from the list above to manage their schedule.</p>
    </div>

    <!-- State 2: Active Workspace -->
    <div v-else class="workspace">
      
      <!-- Doctor Card -->
      <div class="doc-card shadow-sm">
        <div class="doc-avatar">
          {{ getInitials(selectedDoctor?.firstName + ' ' + selectedDoctor?.lastName) }}
        </div>
        <div class="doc-info">
          <h2>Dr. {{ selectedDoctor?.firstName }} {{ selectedDoctor?.lastName }}</h2>
          <span class="doc-spec">{{ selectedDoctor?.specialtyName || 'General Practitioner' }}</span>
        </div>
        <div class="doc-meta">
            <div class="stat">
                <span class="label">Working Days</span>
                <span class="value">{{ groupedSlots.length }}</span>
            </div>
            <div class="stat">
                 <span class="label">Status</span>
                 <span class="value active">Active</span>
            </div>
        </div>
      </div>

      <!-- Schedule Timeline -->
      <div class="timeline-container">
          <div v-if="isLoadingSlots" class="loading-state">
              <div class="spinner"></div>
              <p>Fetching schedule...</p>
          </div>
          
          <div v-else class="schedule-paginator">
              <div class="paginator-header">
                  <button class="nav-btn" @click="prevWeek" :disabled="currentStartDate <= today">
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" width="18"><path d="M15 19l-7-7 7-7"/></svg>
                  </button>
                  <span class="range-text">{{ currentWeekRange }}</span>
                  <button class="nav-btn" @click="nextWeek" :disabled="isLoadingSlots">
                      <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="3" width="18"><path d="M9 5l7 7-7 7"/></svg>
                  </button>
              </div>

              <div class="timeline vertical">
                  <div v-for="day in groupedSlots" :key="day.date" class="day-row">
                      <div class="day-info">
                          <span class="day-name">{{ day.displayDate.split(',')[0] }}</span>
                          <span class="day-val">{{ day.displayDate.split(',')[1] }}</span>
                          <button 
                            v-if="day.slots.some(s => s.isAvailable)"
                            class="btn-select-day" 
                            :class="{ 'is-active': isDayFull(day) }"
                            @click="toggleDaySlots(day)"
                          >
                            {{ isDayFull(day) ? 'Clear Day' : 'Select Day' }}
                          </button>
                      </div>
                      <div class="day-slots">
                          <template v-if="day.slots.length > 0">
                              <button 
                                v-for="slot in day.slots" 
                                :key="slot.startTime"
                                class="slot-btn"
                                :class="{ 
                                    'booked': !slot.isAvailable, 
                                    'available': slot.isAvailable,
                                    'selected': isSelected(slot)
                                }"
                                @click="toggleSlot(slot)"
                              >
                                 {{ new Date(slot.startTime).toLocaleTimeString([], {hour: '2-digit', minute:'2-digit'}) }}
                              </button>
                          </template>
                          <div v-else class="no-slots-day">Closed / No Slots</div>
                      </div>
                  </div>
              </div>
          </div>
      </div>

      <!-- Action Footer -->
      <div class="action-footer shadow-sm" :class="{ 'visible': form.selectedSlots.length > 0 }">
          <div class="footer-content">
              <div class="selection-info">
                  <strong>{{ form.selectedSlots.length }}</strong> slots selected
              </div>
              <input 
                type="text" 
                v-model="form.reason" 
                placeholder="Reason for blocking (e.g. Sick Leave)..." 
                class="reason-input"
              >
              <button @click="submitBlock" class="btn-block" :disabled="isSubmitting">
                 {{ isSubmitting ? 'Processing...' : 'Mark Unavailable' }}
              </button>
          </div>
      </div>

    </div>
  </div>
</template>

<script setup>
import { ref, watch, computed, onMounted } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import { toLocalDateString } from '@/utils/dateFormatter'

const auth = useAuthStore()
const alerts = useAlertStore()

const doctors = ref([])
const allSlots = ref([])
const isLoadingDoctors = ref(false)
const isLoadingSlots = ref(false)
const isSubmitting = ref(false)

const form = ref({
  doctorId: '',
  reason: '',
  selectedSlots: []
})

// --- Date Logic ---
const today = new Date()
today.setHours(0, 0, 0, 0)
const currentStartDate = ref(new Date(today))

const nextWeek = () => {
    const next = new Date(currentStartDate.value)
    next.setDate(next.getDate() + 7)
    currentStartDate.value = next
}

const prevWeek = () => {
    const prev = new Date(currentStartDate.value)
    prev.setDate(prev.getDate() - 7)
    if (prev < today) {
        currentStartDate.value = new Date(today)
    } else {
        currentStartDate.value = prev
    }
}

const currentWeekRange = computed(() => {
    const start = currentStartDate.value
    const end = new Date(start)
    end.setDate(start.getDate() + 6)
    return `${start.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} - ${end.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`
})

// --- Fetch Doctors ---
const fetchDoctors = async () => {
  isLoadingDoctors.value = true
  try {
    const res = await axios.get(`/api/doctors`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    doctors.value = res.data
  } catch (err) {
    alerts.showAlert("Failed to load providers list.", "error")
  } finally {
    isLoadingDoctors.value = false
  }
}

// --- Fetch Schedule ---
const fetchSchedule = async () => {
  if (!form.value.doctorId) return
  
  isLoadingSlots.value = true
  allSlots.value = []
  form.value.selectedSlots = [] 
  
  try {
    const res = await axios.get(`/api/doctors/${form.value.doctorId}/full-schedule`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    allSlots.value = res.data
  } catch (err) {
    alerts.showAlert("Failed to load provider schedule.", "error")
  } finally {
    isLoadingSlots.value = false
  }
}

// --- Display Logic ---
const selectedDoctor = computed(() => {
    return doctors.value.find(d => d.id === form.value.doctorId)
})

const groupedSlots = computed(() => {
    if (allSlots.value.length === 0) return []
    
    const startRange = currentStartDate.value
    const endRange = new Date(startRange)
    endRange.setDate(startRange.getDate() + 7)

    // Group slots by date key "YYYY-MM-DD"
    const groups = {}
    allSlots.value.forEach(slot => {
        const slotDate = new Date(slot.startTime)
        if (slotDate >= startRange && slotDate < endRange) {
            const dateKey = toLocalDateString(slotDate)
            if (!groups[dateKey]) groups[dateKey] = []
            groups[dateKey].push(slot)
        }
    })
    
    // Ensure all 7 days are represented even if empty
    const result = []
    for (let i = 0; i < 7; i++) {
        const d = new Date(startRange)
        d.setDate(startRange.getDate() + i)
        const dateKey = toLocalDateString(d)
        result.push({
            date: dateKey,
            displayDate: d.toLocaleDateString('en-US', { weekday: 'short', month: 'short', day: 'numeric' }),
            slots: groups[dateKey] || []
        })
    }
    return result
})

const getInitials = (name) => {
    if (!name) return ''
    return name.split(' ').map(n => n[0]).join('').substring(0, 2).toUpperCase()
}

// --- Interaction ---
const toggleSlot = (slot) => {
  const index = form.value.selectedSlots.findIndex(s => s.startTime === slot.startTime)
  if (index === -1) {
    form.value.selectedSlots.push(slot)
  } else {
    form.value.selectedSlots.splice(index, 1)
  }
}

const toggleDaySlots = (day) => {
  const availableSlots = day.slots.filter(s => s.isAvailable)
  const allSelected = availableSlots.every(s => isSelected(s))
  
  if (allSelected) {
    // Deselect all for this day
    availableSlots.forEach(s => {
      const idx = form.value.selectedSlots.findIndex(ss => ss.startTime === s.startTime)
      if (idx !== -1) form.value.selectedSlots.splice(idx, 1)
    })
  } else {
    // Select all available for this day
    availableSlots.forEach(s => {
      if (!isSelected(s)) form.value.selectedSlots.push(s)
    })
  }
}

const isDayFull = (day) => {
    const availableSlots = day.slots.filter(s => s.isAvailable)
    if (availableSlots.length === 0) return false
    return availableSlots.every(s => isSelected(s))
}

const isSelected = (slot) => {
  return form.value.selectedSlots.some(s => s.startTime === slot.startTime)
}

const submitBlock = async () => {
  if (form.value.selectedSlots.length === 0) {
    alerts.showAlert("Please select time slots to block.", "warning")
    return
  }
  if (!form.value.reason) {
    alerts.showAlert("Please provide a reason.", "warning")
    return
  }

  isSubmitting.value = true
  
  // Group logic (same as before)
  const sortedSlots = [...form.value.selectedSlots].sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
  const ranges = []
  if (sortedSlots.length > 0) {
    let currentStart = sortedSlots[0].startTime
    let currentEnd = sortedSlots[0].endTime
    for (let i = 1; i < sortedSlots.length; i++) {
        const slot = sortedSlots[i]
        if (new Date(slot.startTime).getTime() === new Date(currentEnd).getTime()) {
            currentEnd = slot.endTime
        } else {
            ranges.push({ start: currentStart, end: currentEnd })
            currentStart = slot.startTime
            currentEnd = slot.endTime
        }
    }
    ranges.push({ start: currentStart, end: currentEnd })
  }

  let failCount = 0
  for (const range of ranges) {
    try {
        await axios.post(`/api/receptionist/doctor/block`, {
            doctorId: parseInt(form.value.doctorId), // Use parsed int
            startTime: range.start,
            endTime: range.end,
            reason: form.value.reason
        }, { headers: { Authorization: `Bearer ${auth.token}` } })
    } catch (err) { failCount++ }
  }

  isSubmitting.value = false
  
  if (failCount === 0) {
      alerts.showAlert(`Blocked ${form.value.selectedSlots.length} slots successfully.`, "success")
      form.value.selectedSlots = []
      form.value.reason = ''
      fetchSchedule()
  } else {
      alerts.showAlert(`Some blocks failed.`, "warning")
      fetchSchedule()
  }
}

watch(() => form.value.doctorId, fetchSchedule)
onMounted(fetchDoctors)
</script>

<style scoped>
.provider-availability { max-width: 1200px; margin: 0 auto; min-height: 80vh; display: flex; flex-direction: column; }

.page-header { margin-bottom: 20px; }
.page-title { font-size: 1.8rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.page-subtitle { color: var(--text-secondary); }

/* Control Bar */
.control-bar {
    background: var(--bg-card); padding: 15px 20px; border-radius: 0;
    border: 1px solid var(--border-default); margin-bottom: 20px;
}
.search-group { display: flex; align-items: center; gap: 15px; }
.search-group label { font-weight: 700; color: var(--text-secondary); }
.doctor-select { 
    flex: 1; max-width: 400px; padding: 10px; border-radius: 0; 
    border: 1px solid var(--border-input); background: var(--bg-input); 
    color: var(--text-primary); font-size: 1rem;
}

/* Empty State */
.empty-state { 
    text-align: center; padding: 60px; background: var(--bg-card); 
    border-radius: 0; border: 2px dashed var(--border-default);
    color: var(--text-secondary); margin-top: 20px;
}
.empty-icon { 
    width: 60px; height: 60px; background: var(--bg-input); border-radius: 0; 
    margin: 0 auto 20px; opacity: 0.5;
}

/* Doctor Card */
.doc-card {
    background: var(--bg-card); padding: 20px; border-radius: 0; 
    border: 1px solid var(--border-default); display: flex; align-items: center; 
    gap: 20px; margin-bottom: 20px; animation: slideDown 0.3s ease;
}
@keyframes slideDown { from { transform: translateY(-10px); opacity: 0; } to { transform: translateY(0); opacity: 1; } }

.doc-avatar {
    width: 60px; height: 60px; border-radius: 0; background: var(--color-primary); 
    color: white; display: flex; align-items: center; justify-content: center; 
    font-weight: 800; font-size: 1.2rem;
}
.doc-info { flex: 1; }
.doc-info h2 { margin: 0; font-size: 1.2rem; color: var(--text-primary); }
.doc-spec { color: var(--color-primary); font-weight: 700; font-size: 0.9rem; text-transform: uppercase; }

.doc-meta { display: flex; gap: 30px; }
.stat { display: flex; flex-direction: column; align-items: flex-end; }
.stat .label { font-size: 0.75rem; color: var(--text-secondary); text-transform: uppercase; font-weight: 700; }
.stat .value { font-weight: 800; color: var(--text-primary); }
.stat .value.active { color: #10b981; }

/* Timeline & Paginator */
.timeline-container { 
    flex: 1; padding-bottom: 20px; 
}
.schedule-paginator {
    background: var(--bg-card); border-radius: 0; border: 1px solid var(--border-default);
    overflow: hidden;
}
.paginator-header {
    display: flex; align-items: center; justify-content: space-between;
    padding: 15px 25px; border-bottom: 1px solid var(--border-default);
    background: var(--bg-input);
}
.nav-btn {
    width: 36px; height: 36px; border-radius: 0; border: 1px solid var(--border-default);
    background: var(--bg-card); cursor: pointer; display: flex; align-items: center; justify-content: center;
    transition: 0.2s; color: var(--text-primary);
}
.nav-btn:hover:not(:disabled) { border-color: var(--color-primary); color: var(--color-primary); transform: scale(1.1); }
.nav-btn:disabled { opacity: 0.3; cursor: not-allowed; }
.range-text { font-weight: 800; font-size: 1.1rem; color: var(--text-primary); }

.timeline.vertical { display: flex; flex-direction: column; gap: 0; padding-bottom: 0; }

.day-row { 
    display: grid; grid-template-columns: 140px 1fr; border-bottom: 1px solid var(--border-default);
}
.day-row:last-child { border-bottom: none; }

.day-info { 
    padding: 20px; background: var(--bg-input); border-right: 1px solid var(--border-default);
    display: flex; flex-direction: column; align-items: center; justify-content: center; gap: 8px;
}
.day-name { font-weight: 800; font-size: 0.8rem; text-transform: uppercase; color: var(--text-secondary); }
.day-val { font-weight: 800; font-size: 1.1rem; color: var(--text-primary); text-align: center; }

.btn-select-day {
    background: none; border: 1.5px solid var(--border-default); padding: 4px 10px;
    border-radius: 0; font-size: 0.65rem; font-weight: 800; text-transform: uppercase;
    cursor: pointer; transition: 0.2s; color: var(--text-secondary);
}
.btn-select-day:hover { border-color: var(--color-primary); color: var(--color-primary); }
.btn-select-day.is-active { background: var(--color-primary); border-color: var(--color-primary); color: white; }

.day-slots { padding: 20px; display: grid; grid-template-columns: repeat(auto-fill, minmax(85px, 1fr)); gap: 8px; }
.no-slots-day { color: var(--text-tertiary); font-style: italic; font-size: 0.85rem; grid-column: 1 / -1; }

.slot-btn {
    padding: 8px 5px; border-radius: 4px; border: 1.5px solid var(--border-default);
    background: var(--bg-card); color: var(--text-primary); font-size: 0.75rem;
    cursor: pointer; transition: 0.1s; text-align: center; font-weight: 700;
}
.slot-btn:hover { border-color: var(--color-primary); }
.slot-btn.available { border-left: 2px solid #10b981; }
.slot-btn.booked { border-left: 2px solid #f59e0b; background: #fffbeb; opacity: 0.8; }

.slot-btn.selected { 
    background: var(--color-primary); color: white; border-color: var(--color-primary); 
    transform: scale(1.05); box-shadow: 0 4px 12px rgba(0, 179, 158, 0.3);
}

/* Action Footer */
.action-footer {
    position: fixed; bottom: 0; left: 0; right: 0; 
    background: var(--bg-card); border-top: 1px solid var(--border-default);
    padding: 15px; transform: translateY(100%); transition: transform 0.3s cubic-bezier(0.4, 0, 0.2, 1);
    z-index: 100;
}
.action-footer.visible { transform: translateY(0); }

.footer-content { 
    max-width: 1000px; margin: 0 auto; display: flex; align-items: center; gap: 20px; 
}
.selection-info { font-weight: 700; color: var(--text-primary); min-width: 120px; }
.reason-input { flex: 1; padding: 10px 15px; border-radius: 0; border: 2px solid var(--border-input); background: var(--bg-input); outline: none; }
.reason-input:focus { border-color: var(--color-primary); }

.btn-block {
    background: #ef4444; color: white; padding: 10px 25px; border-radius: 0;
    font-weight: 800; border: none; cursor: pointer; transition: 0.2s;
}
.btn-block:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(239, 68, 68, 0.3); }

.loading-state { text-align: center; padding: 40px; color: var(--text-secondary); }
.spinner { width: 30px; height: 30px; border: 3px solid #ccc; border-top-color: var(--color-primary); border-radius: 50%; animation: spin 1s infinite; margin: 0 auto 10px; }
@keyframes spin { to { transform: rotate(360deg); } }
</style>
