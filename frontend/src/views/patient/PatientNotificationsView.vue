<template>
  <div class="health-hub-page">
    <header class="page-header">
      <div class="header-main">
        <h1>Patient <span>Hub</span></h1>
        <p>Medical alerts and portal communications.</p>
      </div>
    </header>

    <div class="hub-container">
      <!-- Top Navigation Tabs -->
      <nav class="hub-tabs">
        <button 
          v-for="tab in hubTabs" 
          :key="tab.id"
          :class="['tab-btn', { active: activeTab === tab.id }]"
          @click="activeTab = tab.id"
        >
          <span class="tab-text">{{ tab.label }}</span>
          <span v-if="tab.id === 'new' && auth.unreadNotificationCount > 0" class="tab-badge">{{ auth.unreadNotificationCount }}</span>
        </button>
      </nav>

      <!-- Content Area -->
      <main class="hub-content">
        <section class="content-section">
          <div class="section-header">
            <div class="header-actions">
              <button @click="fetchNotifications" class="btn-refresh" :disabled="isLoading">
                {{ isLoading ? 'Refresh' : 'Refresh' }}
              </button>
              <button v-if="activeTab === 'new' && notifications.length > 0" @click="markAllAsRead" class="btn-text">Clear All</button>
            </div>
          </div>
          
          <div class="alerts-list">
            <template v-if="isLoading">
              <div v-for="i in 3" :key="i" class="alert-card skeleton-card">
                <div class="alert-top-row">
                  <Skeleton width="80px" height="24px" borderRadius="8px" />
                  <Skeleton width="100px" height="18px" />
                </div>
                <Skeleton width="60%" height="1.25rem" style="margin-top: 10px" />
                <Skeleton width="100%" height="1rem" style="margin-top: 12px" />
                <Skeleton width="100%" height="1rem" style="margin-top: 5px" />
                <div class="alert-footer-meta">
                  <Skeleton width="150px" height="18px" />
                  <Skeleton width="100px" height="36px" borderRadius="10px" />
                </div>
              </div>
            </template>
            
            <div v-else-if="notifications.length === 0" class="empty-state">
              <p>{{ activeTab === 'new' ? 'No new messages.' : 'No history.' }}</p>
            </div>
    
            <div 
              v-else
              v-for="alert in notifications" 
              :key="alert.id"
              class="alert-card"
            >
              <div class="alert-body">
                <div class="alert-top-row">
                    <span v-if="alert.type" class="alert-type-tag" :class="alert.type.replace(/\s+/g, '-').toLowerCase()">{{ alert.type }}</span>
                    <span v-else class="alert-type-tag general">General</span>
                    <span class="alert-time">{{ formatTime(alert.createdAt) }}</span>
                </div>
                
                <h3 class="alert-title">{{ alert.title }}</h3>
                <p class="alert-message">{{ alert.message }}</p>
                
                <div class="alert-footer-meta">
                  <span class="alert-sender-label">From: <span class="sender-name">{{ alert.senderName || 'Hospital Staff' }}</span></span>
                  
                  <div class="alert-actions">
                      <button 
                        v-if="shouldShowConfirmButton(alert)" 
                        class="confirm-btn-action" 
                        @click="handleConfirm(alert)"
                      >
                        Confirm Attendance
                      </button>
                      
                      <button 
                        v-if="activeTab === 'new'" 
                        class="btn-read-action" 
                        @click="markAsRead(alert.id)"
                      >
                        {{ getButtonLabel(alert.type) }}
                      </button>
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>
      </main>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from 'vue'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { useAlertStore } from '@/stores/alertStore'
import Skeleton from '@/components/Skeleton.vue'

const auth = useAuthStore()
const alertStore = useAlertStore()

const activeTab = ref('new')
const isLoading = ref(false)

const hubTabs = [
  { id: 'new', label: 'New' },
  { id: 'history', label: 'History' }
]

const notifications = ref([])

const fetchNotifications = async () => {
  isLoading.value = true
  try {
    const endpoint = activeTab.value === 'new' ? '/api/notifications/unread' : '/api/notifications/history'
    const res = await axios.get(`${endpoint}`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    notifications.value = res.data || []
    auth.fetchUnreadCount()
  } catch (err) {
    console.error('Failed to fetch notifications', err)
  } finally {
    isLoading.value = false
  }
}

watch(activeTab, () => {
    fetchNotifications()
})

const markAsRead = async (id) => {
  try {
    // Immediate local feedback
    if (activeTab.value === 'new') {
        notifications.value = notifications.value.filter(n => n.id !== id)
    }
    
    await axios.put(`/api/notifications/${id}/read`, {}, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    auth.fetchUnreadCount()
  } catch (err) {
    console.error('Failed to mark as read', err)
  }
}

const markAllAsRead = async () => {
  try {
    if (activeTab.value === 'new') {
        notifications.value = []
    }
    await axios.put(`/api/notifications/read-all`, {}, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    auth.fetchUnreadCount()
  } catch (err) {
    console.error('Failed to mark all as read', err)
  }
}

const handleConfirm = async (alert) => {
    try {
        await axios.post(`/api/notifications/confirm-appointment/${alert.appointmentId}`, {}, {
            headers: { Authorization: `Bearer ${auth.token}` }
        })
        alertStore.showAlert('Attendance confirmed.', 'success')
        // Move to history immediately
        markAsRead(alert.id)
    } catch (err) {
        alertStore.showAlert('Failed to confirm.', 'error')
    }
}

const getButtonLabel = (type) => {
    if (type === 'Reminder') return 'Got it'
    if (type === 'Status Update') return 'Acknowledge'
    return 'Dismiss'
}

const shouldShowConfirmButton = (alert) => {
    if (alert.type !== 'Appointment') return false
    if (alert.isConfirmed) return false
    
    // Check status
    if (alert.appointmentStatus && alert.appointmentStatus.toLowerCase() === 'cancelled') return false
    
    // Check time
    if (alert.appointmentStartTime) {
        const apptTime = new Date(alert.appointmentStartTime)
        // If appointment time is in the past, don't show confirm
        if (apptTime < new Date()) return false
    }
    
    return true
}

const formatTime = (iso) => {
  return new Date(iso).toLocaleDateString('en-US', { month: 'short', day: 'numeric', hour: '2-digit', minute: '2-digit' })
}

onMounted(fetchNotifications)
</script>

<style scoped>
.health-hub-page {
  max-width: 75%;
  margin: 0 auto;
  padding: 1.5rem 15px;
}

/* Header */
.page-header {
  margin-bottom: 2.5rem;
  padding: 0 5px;
}
.header-main h1 { font-size: 2.2rem; font-weight: 800; color: var(--text-primary); margin: 0; letter-spacing: -1px; }
.header-main h1 span { color: var(--color-primary); }
.header-main p { color: var(--text-secondary); font-size: 1rem; margin-top: 5px; }

/* Hub Layout */
.hub-container {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* Tabs Navigation */
.hub-tabs {
  display: flex;
  gap: 8px;
  background: var(--bg-card);
  padding: 6px;
  border-radius: 12px;
  border: 1px solid var(--border-default);
}

.tab-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  padding: 10px;
  border: none;
  background: none;
  border-radius: 8px;
  cursor: pointer;
  font-weight: 700;
  color: var(--text-secondary);
  font-size: 0.95rem;
}

.tab-btn.active { background: var(--color-primary); color: white; }

.tab-badge {
  background: #ef4444;
  color: white;
  font-size: 0.7rem;
  padding: 2px 6px;
  border-radius: 50px;
}

/* Content Area */
.hub-content {
  background: var(--bg-card);
  padding: 24px;
  box-shadow: var(--shadow-sm);
}

.section-header {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 20px;
}

/* Alerts List */
.alerts-list { display: flex; flex-direction: column; gap: 12px; }

.alert-card {
  padding: 20px;
  margin: 1rem 0;
  border-radius: 12px;
  border: 1px solid var(--border-default);
  background-color: var(--bg-input);
  position: relative;
  transition: transform 0.2s, box-shadow 0.2s;
}

.alert-card:hover {
    transform: translateY(-2px);
    box-shadow: var(--shadow-md);
}

.alert-top-row { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }

.alert-title { font-size: 1.25rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.alert-message { font-size: 1.05rem; color: var(--text-secondary); margin: 12px 0; line-height: 1.6; }

.alert-footer-meta {
    display: flex;
    justify-content: space-between;
    align-items: center;
    margin-top: 18px;
    padding-top: 18px;
    border-top: 1px solid var(--border-default);
}

.alert-sender-label { font-size: 0.95rem; color: var(--text-secondary); }
.sender-name { font-weight: 700; color: var(--text-primary); }

.alert-actions { display: flex; gap: 8px; }

.confirm-btn-action, .btn-read-action {
    padding: 8px 20px;
    border-radius: 10px;
    font-size: 0.95rem;
    font-weight: 700;
    cursor: pointer;
}

.confirm-btn-action { background: var(--color-primary); color: white; border: none; }
.btn-read-action { background: var(--bg-input); border: 1px solid var(--border-default); color: var(--text-primary); }
.btn-read-action:hover { border-color: var(--color-primary); color: var(--color-primary); }

.btn-text { background: none; border: none; color: var(--color-primary); font-weight: 800; cursor: pointer; font-size: 0.95rem; }
.btn-refresh {
    background: var(--bg-input);
    border: 1px solid var(--border-default);
    padding: 8px 16px;
    border-radius: 10px;
    font-size: 0.95rem;
    font-weight: 700;
    cursor: pointer;
    margin-right: 10px;
}

.alert-type-tag { font-size: 0.75rem; font-weight: 800; text-transform: uppercase; padding: 4px 10px; border-radius: 8px; }
.alert-type-tag.appointment { background: var(--nt-appointment-bg); color: var(--nt-appointment-text); }
.alert-type-tag.reminder { background: var(--nt-reminder-bg); color: var(--nt-reminder-text); }
.alert-type-tag.action-required { background: var(--nt-action-bg); color: var(--nt-action-text); }
.alert-type-tag.status-update { background: var(--nt-status-bg); color: var(--nt-status-text); }
.alert-type-tag.administrative { background: var(--nt-admin-bg); color: var(--nt-admin-text); }
.alert-type-tag.general { background: var(--nt-general-bg); color: var(--nt-general-text); }

.alert-time { font-size: 0.9rem; color: var(--text-tertiary); }

@keyframes spin { to { transform: rotate(360deg); } }
.loading-state, .empty-state { text-align: center; padding: 50px 20px; color: var(--text-secondary); font-size: 1rem; }
.spinner { width: 24px; height: 24px; border: 3px solid var(--border-default); border-top-color: var(--color-primary); border-radius: 50%; animation: spin 1s linear infinite; margin: 0 auto; }

@media (max-width: 600px) {
    .alert-footer-meta { flex-direction: column; gap: 10px; align-items: flex-start; }
    .alert-actions { width: 100%; justify-content: flex-end; }
}
</style>