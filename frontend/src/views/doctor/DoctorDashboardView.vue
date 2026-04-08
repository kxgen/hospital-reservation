<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import axios from '@/api/axios'
import { useAuthStore } from '@/stores/auth'
import { formatTime } from '@/utils/dateFormatter'
import Skeleton from '@/components/Skeleton.vue'

const router = useRouter()
const auth = useAuthStore()

// State
const allTodayAppointments = ref([])
const checkedInPatients = ref([])
const loading = ref(true)

const totalToday = computed(() => allTodayAppointments.value.length)
const completedToday = computed(() => allTodayAppointments.value.filter(a => a.status.toLowerCase() === 'completed').length)

const progressPercent = computed(() => {
    if (totalToday.value === 0) return 0
    return Math.round((completedToday.value / totalToday.value) * 100)
})

const todayAgenda = computed(() => {
  return [...allTodayAppointments.value]
    .sort((a, b) => new Date(a.startTime) - new Date(b.startTime))
    .slice(0, 10)
})

const fetchDashboardData = async () => {
  try {
    loading.value = true
    const res = await axios.get(`/api/appointments/doctor-schedule`, {
      headers: { Authorization: `Bearer ${auth.token}` }
    })
    
    const today = new Date().toISOString().split('T')[0]
    const raw = res.data.filter(a => {
      const s = a.status.toLowerCase()
      return s !== 'cancelled'
    })
    
    // All appointments for today (for stats)
    allTodayAppointments.value = raw.filter(a => a.startTime.split('T')[0] === today)
    
    // Only checked-in patients (not yet completed)
    checkedInPatients.value = allTodayAppointments.value
      .filter(a => a.checkedInAt != null && a.status.toLowerCase() !== 'completed')
      .sort((a, b) => new Date(a.startTime) - new Date(b.startTime))

  } catch (err) {
    console.error('Failed to load dashboard data', err)
  } finally {
    loading.value = false
  }
}

const beginSession = (aptId) => {
    router.push({ name: 'doctor-appointment-detail', params: { id: aptId } })
}

onMounted(fetchDashboardData)
</script>

<template>
  <div class="doctor-container">
    <div class="page-header">
      <div class="header-content">
        <h1>Welcome back, <span>Dr. {{ auth.name }}</span></h1>
        <p>Here is your patient queue for today.</p>
      </div>
      
      <div class="header-actions">
        <div class="progress-ribbon">
          <div class="progress-info">
            <div class="stat-main">
              <span class="label">Consultation Progress</span>
              <span class="count"><strong>{{ completedToday }}</strong> / {{ totalToday }} Patients</span>
            </div>
            <div class="percent-tag">{{ progressPercent }}%</div>
          </div>
          <div class="progress-bar">
            <div class="progress-fill" :style="{ width: progressPercent + '%' }"></div>
          </div>
        </div>
      </div>
    </div>

    <main class="dashboard-content">
      <section class="queue-section">
        <div class="section-header">
          <h2>Checked-in Patients</h2>
          <span class="queue-count">{{ checkedInPatients.length }} Waiting</span>
        </div>

        <div v-if="loading" class="patient-grid">
           <div v-for="i in 3" :key="i" class="patient-card">
              <div class="card-left">
                <Skeleton width="90px" height="36px" borderRadius="10px" />
                <div class="patient-info">
                  <Skeleton width="150px" height="1.1rem" style="margin-bottom: 5px" />
                  <Skeleton width="200px" height="0.85rem" />
                </div>
              </div>
              <Skeleton width="120px" height="40px" borderRadius="12px" />
           </div>
        </div>

        <div v-else-if="checkedInPatients.length > 0" class="patient-grid">
          <div v-for="apt in checkedInPatients" :key="apt.id" class="patient-card">
            <div class="card-left">
              <div class="time-badge">{{ formatTime(apt.startTime) }}</div>
              <div class="patient-info">
                <h3>{{ apt.patientName }}</h3>
                <p class="appointment-type">{{ apt.appointmentType }} • {{ apt.reason || 'General Checkup' }}</p>
              </div>
            </div>
            <button class="btn-begin" @click="beginSession(apt.id)">
              Begin Session
              <svg class="icon-right" viewBox="0 0 24 24" width="18" height="18"><path fill="currentColor" d="M8.59,16.58L13.17,12L8.59,7.41L10,6L16,12L10,18L8.59,16.58Z"/></svg>
            </button>
          </div>
        </div>

        <div v-else class="empty-queue">
          <div class="empty-icon">🛋️</div>
          <h3>Queue is clear</h3>
          <p>No patients are currently checked in. Take a moment to review previous records.</p>
          <button class="btn-secondary" @click="$router.push('/doctor/appointments')">View Full Schedule</button>
        </div>
      </section>

      <aside v-if="!loading" class="quick-info">
         <div class="next-up-card">
            <h3>Schedule Insight</h3>
            <div class="insight-content">
                <div class="insight-item">
                    <span class="i-label">Next Scheduled</span>
                    <span class="i-value">
                      {{ allTodayAppointments.find(a => a.status.toLowerCase() === 'scheduled')?.startTime ? formatTime(allTodayAppointments.find(a => a.status.toLowerCase() === 'scheduled').startTime) : 'None' }}
                    </span>
                </div>
                <div class="insight-item">
                    <span class="i-label">Total Appointments</span>
                    <span class="i-value">{{ totalToday }}</span>
                </div>
            </div>
         </div>
      </aside>
    </main>

    <section class="agenda-section full-width-section">
      <div class="section-header">
        <h2>Today's Full Agenda</h2>
        <span class="queue-count">{{todayAgenda.length}} appts</span>
      </div>

      <div v-if="loading" class="agenda-list">
        <div v-for="i in 5" :key="i" class="agenda-row skeleton">
           <Skeleton width="60px" height="20px" />
           <Skeleton width="150px" height="20px" />
           <Skeleton width="80px" height="20px" borderRadius="10px" />
        </div>
      </div>
      <div v-else-if="todayAgenda.length > 0" class="agenda-list">
        <div v-for="apt in todayAgenda" :key="apt.id" class="agenda-row" :class="{ completed: apt.status.toLowerCase() === 'completed' }">
          <div class="a-time">{{ formatTime(apt.startTime) }}</div>
          <div class="a-patient">
            <span class="a-name">{{ apt.patientName }}</span>
            <span class="a-reason">{{ apt.reason || 'Checkup' }}</span>
          </div>
          <div class="a-status">
            <span :class="['status-pill', apt.status.toLowerCase()]">{{ apt.status }}</span>
          </div>
          <button class="btn-mini-action" @click="beginSession(apt.id)">
            <svg viewBox="0 0 24 24" width="16" height="16"><path fill="currentColor" d="M12,9A3,3 0 0,0 9,12A3,3 0 0,0 12,15A3,3 0 0,0 15,12A3,3 0 0,0 12,9M12,17A5,5 0 0,1 7,12A5,5 0 0,1 12,7A5,5 0 0,1 17,12A5,5 0 0,1 12,17M12,4.5C7,4.5 2.73,7.61 1,12C2.73,16.39 7,19.5 12,19.5C17,19.5 21.27,16.39 23,12C21.27,7.61 17,4.5 12,4.5Z"/></svg>
          </button>
        </div>
      </div>
      <div v-else class="empty-agenda">
        <p>No appointments scheduled for the remainder of the day.</p>
      </div>
    </section>
  </div>
</template>

<style scoped>
.doctor-container {
  padding: 0 20px;
}

/* Header & Progress */
.dashboard-header {
  margin-bottom: 40px;
  display: flex;
  flex-direction: column;
  gap: 30px;
}

.welcome-text h1 { font-size: 2.2rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.welcome-text h1 span { color: var(--color-primary); }
.welcome-text p { color: var(--text-secondary); font-size: 1.1rem; margin-top: 5px; }

.progress-ribbon {
  background: var(--bg-card);
  padding: 15px 20px;
  border-radius: 0;
  border: 1px solid var(--border-default);
  width: 280px;
}

.progress-info {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 15px;
}

.stat-main { display: flex; flex-direction: column; gap: 4px; }
.stat-main .label { font-size: 0.75rem; font-weight: 800; text-transform: uppercase; color: var(--text-tertiary); letter-spacing: 1px; }
.stat-main .count { font-size: 1.1rem; color: var(--text-primary); }
.stat-main .count strong { color: var(--color-primary); }

.percent-tag {
  background: var(--color-primary-light);
  color: var(--color-primary);
  padding: 6px 12px;
  border-radius: 10px;
  font-weight: 800;
  font-size: 0.9rem;
}

.progress-bar {
  height: 6px;
  background: var(--bg-input);
  border-radius: 0;
  overflow: hidden;
}

.progress-fill {
  height: 100%;
  background: var(--color-primary);
  transition: width 0.8s cubic-bezier(0.34, 1.56, 0.64, 1);
}

/* Main Content */
.dashboard-content {
  display: grid;
  grid-template-columns: 1fr 300px;
  gap: 40px;
  align-items: start;
}

.section-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 25px;
}

.section-header h2 { font-size: 1.4rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.queue-count { 
  background: var(--bg-input); 
  padding: 4px 12px; 
  border-radius: 50px; 
  font-size: 0.8rem; 
  font-weight: 700; 
  color: var(--text-secondary);
}

.patient-grid {
  display: flex;
  flex-direction: column;
  gap: 15px;
}

.patient-card {
  display: flex;
  justify-content: space-between;
  align-items: center;
  background: var(--bg-card);
  padding: 15px 20px;
  border-radius: 0;
  border: 1px solid var(--border-default);
  transition: all 0.2s ease;
}

.patient-card:hover { 
  border-color: var(--color-primary-border); 
  transform: translateX(5px);
  box-shadow: var(--shadow-md);
}

.card-left { display: flex; align-items: center; gap: 20px; }

.time-badge {
  background: var(--bg-body);
  border: 1px solid var(--border-default);
  padding: 6px 10px;
  border-radius: 0;
  font-weight: 800;
  color: var(--color-primary);
  font-size: 0.85rem;
  min-width: 80px;
  text-align: center;
}

.patient-info h3 { font-size: 1.1rem; font-weight: 800; color: var(--text-primary); margin: 0; }
.appointment-type { font-size: 0.85rem; color: var(--text-secondary); margin-top: 2px; }

.btn-begin {
  background: var(--color-primary);
  color: white;
  border: none;
  padding: 8px 16px;
  border-radius: 0;
  font-weight: 700;
  display: flex;
  align-items: center;
  gap: 8px;
  cursor: pointer;
  transition: 0.2s;
}

.btn-begin:hover { background: var(--color-primary-hover); transform: scale(1.05); }
.icon-right { margin-left: 8px; flex-shrink: 0; }

/* Empty State */
.empty-queue {
  background: var(--bg-card);
  padding: 40px;
  border-radius: 0;
  border: 1px dashed var(--border-default);
  text-align: center;
}

.empty-icon { font-size: 3rem; margin-bottom: 20px; opacity: 0.5; }
.empty-queue h3 { font-size: 1.5rem; color: var(--text-primary); margin: 0; }
.empty-queue p { color: var(--text-secondary); margin: 10px 0 25px; }

.btn-secondary {
  background: var(--bg-body);
  border: 1.5px solid var(--border-default);
  padding: 8px 20px;
  border-radius: 0;
  font-weight: 700;
  cursor: pointer;
}

/* Agenda Section */
.agenda-section.full-width-section { 
  margin-top: 50px; 
  padding-top: 30px;
  border-top: 1px solid var(--border-default);
}
.agenda-list { display: flex; flex-direction: column; background: var(--bg-card); border-radius: 0; border: 1px solid var(--border-default); overflow: hidden; box-shadow: var(--shadow-sm); }
.agenda-row { display: grid; grid-template-columns: 80px 1fr 120px 40px; align-items: center; padding: 15px 20px; border-bottom: 1px solid var(--border-default); transition: 0.2s; gap: 10px; }
.agenda-row:last-child { border-bottom: none; }
.agenda-row:hover { background: var(--color-gray-50); }
.agenda-row.completed { opacity: 0.6; background: var(--bg-body); }

.a-time { font-weight: 800; color: var(--color-primary); font-size: 0.85rem; }
.a-patient { display: flex; flex-direction: column; }
.a-name { font-weight: 700; color: var(--text-primary); font-size: 0.95rem; }
.a-reason { font-size: 0.75rem; color: var(--text-secondary); }

.btn-mini-action { background: none; border: none; color: var(--text-tertiary); cursor: pointer; display: flex; align-items: center; justify-content: center; border-radius: 8px; width: 32px; height: 32px; transition: 0.2s; }
.btn-mini-action:hover { background: var(--bg-input); color: var(--color-primary); }

.status-pill { font-size: 0.65rem; padding: 3px 10px; border-radius: 50px; font-weight: 800; text-transform: uppercase; display: inline-block; text-align: center; }
.status-pill.scheduled { background: var(--bg-warning); color: var(--text-warning); }
.status-pill.completed { background: var(--bg-success); color: var(--text-success); }
.status-pill.cancelled { background: var(--bg-error); color: var(--text-error); }
.status-pill.inprogress { background: #e0f2fe; color: #0369a1; }

.empty-agenda { text-align: center; padding: 30px; color: var(--text-secondary); font-size: 0.9rem; font-style: italic; }

/* Sidebar Info */
.next-up-card {
  background: var(--bg-card);
  padding: 25px;
  border-radius: 0;
  border: 1px solid var(--border-default);
}

.next-up-card h3 { font-size: 0.9rem; font-weight: 800; text-transform: uppercase; color: var(--text-tertiary); margin-bottom: 20px; letter-spacing: 1px; }

.insight-content { display: flex; flex-direction: column; gap: 20px; }
.insight-item { display: flex; flex-direction: column; gap: 5px; }
.i-label { font-size: 0.8rem; color: var(--text-secondary); }
.i-value { font-size: 1.2rem; font-weight: 800; color: var(--text-primary); }

@media (max-width: 900px) {
  .dashboard-content { grid-template-columns: 1fr; }
  .dashboard-header { gap: 20px; }
}

.spinner {
  width: 40px; height: 40px; border: 3px solid var(--border-default);
  border-top-color: var(--color-primary); border-radius: 50%;
  animation: spin 1s linear infinite; margin: 0 auto 20px;
}
@keyframes spin { to { transform: rotate(360deg); } }
.loading-state { text-align: center; padding: 40px; color: var(--text-secondary); }
</style>