<script setup>
    import { ref, onMounted } from 'vue'
    import axios from '@/api/axios'
    import { useAuthStore } from '@/stores/auth'
    import { formatTime as formatTimeUtil } from '@/utils/dateFormatter'
    import { useAlertStore } from '@/stores/alertStore'
    import Skeleton from '@/components/Skeleton.vue'

    const alert = useAlertStore()
    const formatTime = (d) => (d ? formatTimeUtil(d) : 'TBD')

    const auth = useAuthStore()
    const patientName = ref('User')
    const nextAppointment = ref(null)
    const loading = ref(true)

    const fetchDashboard = async () => {
      if (!auth.isLoaded) await auth.loadFromStorage()
      if (!auth.token) return
      try {
        loading.value = true
        const response = await axios.get('/api/patient/dashboard', {
          headers: { Authorization: `Bearer ${auth.token}` },
        })
        patientName.value = response.data.patientName
        nextAppointment.value = response.data.nextAppointment
      } catch (err) {
        console.error(err)
      } finally {
        loading.value = false
      }
    }

    const getDay = (d) => (d ? new Date(d).getDate() : '--')
    const getMonth = (d) =>
      d ? new Date(d).toLocaleString('default', { month: 'short' }).toUpperCase() : '...'

    onMounted(fetchDashboard)
</script>

<template>
  <div class="dashboard-page">
    <header class="dashboard-header">
      <div class="container header-flex">
        <div class="welcome-msg">
          <p class="date-today">{{ new Date().toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' }) }}</p>
          <h1 v-if="loading">Welcome, <Skeleton width="200px" height="2.6rem" borderRadius="10px" style="display:inline-block; vertical-align: middle;" /></h1>
          <h1 v-else>Welcome, <span>{{ patientName }}</span></h1>
          
          <div class="status-indicator">
            <template v-if="loading">
              <Skeleton width="180px" height="1rem" />
            </template>
            <template v-else-if="nextAppointment">
              <p class="help-text">You have an appointment coming up soon.</p>
            </template>
            <template v-else>
              <p class="help-text">No upcoming appointments scheduled.</p>
            </template>
          </div>
        </div>
      </div>
    </header>

    <div class="container dashboard-grid">
      <main class="main-content">
        <!-- Hero Section: Next Appointment -->
        <section v-if="nextAppointment" class="hero-appointment-box">
          <div class="hero-header">
            <div class="pulse-wrapper">
              <span class="pulse-icon"></span>
              <span class="pulse-ring"></span>
            </div>
            NEXT UPCOMING APPOINTMENT
          </div>

          <div class="hero-body">
            <div class="hero-date-block">
              <div class="big-date">
                <span class="m">{{ getMonth(nextAppointment.startTime) }}</span>
                <span class="d">{{ getDay(nextAppointment.startTime) }}</span>
                <span class="y">{{ new Date(nextAppointment.startTime).getFullYear() }}</span>
              </div>
              <div class="big-time">
                <span class="time-label">TIME</span>
                {{ formatTime(nextAppointment.startTime) }}
              </div>
            </div>

            <div class="hero-info-block">
              <div class="dr-detail">
                <small class="label-caps">Medical Specialist</small>
                <h3>Dr. {{ nextAppointment.doctorName || 'Name Unknown' }}</h3>
                <p class="specialty">{{ nextAppointment.specialty || 'General Practitioner' }}</p>
                
                <div class="hero-meta">
                  <span :class="['status-tag', 'status-' + (nextAppointment.status || '').toLowerCase()]">
                    <span class="dot"></span>
                    {{ nextAppointment.status }}
                  </span>
                </div>
              </div>
            </div>

            <div class="hero-actions">
               <router-link
                :to="'/patient/edit-appointment/' + nextAppointment.id"
                class="btn-main-green"
              >Manage</router-link>
            </div>
          </div>
        </section>

        <div v-else-if="!loading" class="empty-agenda">
          <div class="empty-icon">📅</div>
          <p>No upcoming appointments.</p>
          <router-link to="/doctors" class="btn-text">Book a screening today</router-link>
        </div>
      </main>

      <aside class="dashboard-sidebar">
        <div class="sidebar-card tip-card">
          <div class="card-icon">💡</div>
          <h3>Helpful Tip</h3>
          <p>Arrive 15 minutes early for check-in and documentation.</p>
        </div>

        <div class="sidebar-card links-card">
          <h3>Quick Links</h3>
          <nav class="side-nav">
            <router-link to="/doctors" class="nav-item">
              <span class="nav-icon">🔍</span> Find Doctors
            </router-link>
            <router-link to="/patient/profile" class="nav-item">
              <span class="nav-icon">👤</span> Profile Settings
            </router-link>
            <router-link to="/patient/notifications" class="nav-item">
              <span class="nav-icon">💬</span> Message Center
            </router-link>
          </nav>
        </div>
      </aside>
    </div>
  </div>
</template>

<style scoped>
/* ==========================================================================
   PAGE BASE
   ========================================================================== */
.dashboard-page {
  background: var(--bg-body);
  min-height: 100vh;
  padding-bottom: 100px;
  color: var(--text-primary);
}

.container {
  width: 90%;
  max-width: 1300px;
  margin: 0 auto;
}

/* ==========================================================================
   HEADER & WELCOME
   ========================================================================== */
.dashboard-header {
  background: var(--bg-card);
  padding: 0 15px;
  border-bottom: 1px solid var(--border-default);
  margin-bottom: 40px;
}

.date-today {
  color: var(--text-secondary);
  font-size: 0.85rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  margin-bottom: 8px;
}

.welcome-msg h1 {
  font-size: 2.6rem;
  font-weight: 800;
  letter-spacing: -1px;
  color: var(--text-primary);
}

.welcome-msg h1 span {
  color: var(--color-primary);
}

.status-indicator {
  margin-top: 10px;
  font-weight: 500;
  color: var(--text-secondary);
}

/* ==========================================================================
   LAYOUT GRID
   ========================================================================== */
.dashboard-grid {
  display: grid;
  grid-template-columns: 1fr 340px;
  gap: 40px;
}

/* ==========================================================================
   HERO APPOINTMENT BOX
   ========================================================================== */
.hero-appointment-box {
  background: var(--bg-card);
  border-radius: 20px;
  overflow: hidden;
  margin-bottom: 50px;
  box-shadow: var(--shadow-md);
  border: 1px solid var(--border-default);
}

.hero-header {
  background: var(--bg-input);
  padding: 14px 30px;
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--color-primary);
  display: flex;
  align-items: center;
  gap: 12px;
  letter-spacing: 1.5px;
  border-bottom: 1px solid var(--border-default);
}

/* Corrected Pulse Animation Centering */
.pulse-wrapper { 
  position: relative; 
  display: flex; 
  align-items: center; 
  justify-content: center;
  width: 24px;
  height: 24px;
}

.pulse-icon { 
  width: 8px; 
  height: 8px; 
  background: var(--color-primary); 
  border-radius: 50%; 
  z-index: 2;
  display: block;
}

.pulse-ring {
  position: absolute; 
  width: 100%; 
  height: 100%; 
  border-radius: 50%;
  border: 3px solid var(--color-primary); 
  opacity: 0;
  animation: pulse-animation 2s infinite;
}

@keyframes pulse-animation {
  0% { transform: scale(0.5); opacity: 0.8; }
  100% { transform: scale(1.5); opacity: 0; }
}

.hero-body {
  padding: 40px 30px;
  display: flex;
  align-items: center;
  flex-wrap: wrap;
  gap: 30px;
}

.hero-date-block {
  text-align: center;
  padding-right: 40px;
  border-right: 1px solid var(--border-default);
  min-width: 140px;
}

.big-date .m { display: block; font-size: 1rem; font-weight: 800; color: var(--color-primary); text-transform: uppercase; }
.big-date .d { display: block; font-size: 3.5rem; font-weight: 900; color: var(--text-primary); line-height: 1; margin: 4px 0; }
.big-date .y { font-size: 0.9rem; color: var(--text-secondary); font-weight: 600; }

.big-time {
  margin-top: 15px; font-weight: 800; font-size: 1.1rem; color: var(--text-primary);
}
.time-label { display: block; font-size: 0.6rem; color: var(--text-secondary); margin-bottom: 2px; }

.hero-info-block { flex: 1; min-width: 250px; }
.label-caps { text-transform: uppercase; color: var(--text-secondary); font-size: 0.7rem; font-weight: 800; letter-spacing: 1px; }
.dr-detail h3 { font-size: 1.8rem; font-weight: 800; margin: 5px 0; color: var(--text-primary); }
.specialty { color: var(--color-primary); font-weight: 700; font-size: 1.1rem; margin-bottom: 15px; }

.hero-actions {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.dot {
  width: 6px;
  height: 6px;
  margin-right: 6px;
  border-radius: 50%;
  background: currentColor;
}

.btn-main-green {
  background: var(--color-primary);
  color: var(--color-white);
  padding: 12px 24px;
  border-radius: 10px;
  text-decoration: none;
  font-weight: 700;
  text-align: center;
  transition: all 0.2s;
  box-shadow: var(--shadow-primary);
}

.btn-main-green:hover {
  background: var(--color-primary-hover);
  transform: translateY(-2px);
}

.empty-agenda {
  text-align: center;
  padding: 60px 40px;
  background: var(--color-gray-50);
  border-radius: 16px;
  border: 1px dashed var(--border-input);
}

.empty-icon { font-size: 2.5rem; margin-bottom: 15px; }
.empty-agenda p { color: var(--text-secondary); font-weight: 600; margin-bottom: 15px; }
.btn-text { color: var(--color-primary); font-weight: 800; text-decoration: none; font-size: 0.95rem; text-underline-offset: 6px;}
.btn-text:hover { text-decoration: underline;}

/* ==========================================================================
   FLOATING ACTION BUTTON
   ========================================================================== */
.fab-btn {
  position: fixed;
  bottom: 40px;
  right: 40px;
  background: var(--color-primary);
  color: var(--color-white);
  padding: 15px 25px;
  border-radius: 50px;
  display: flex;
  align-items: center;
  gap: 12px;
  font-weight: 800;
  text-decoration: none;
  box-shadow: 0 10px 25px rgba(93, 134, 108, 0.4);
  z-index: 100;
  transition: all 0.3s cubic-bezier(0.175, 0.885, 0.32, 1.275);
}

.fab-btn:hover {
  transform: scale(1.05) translateY(-5px);
  background: var(--color-primary-hover);
  box-shadow: 0 15px 30px rgba(93, 134, 108, 0.5);
}

.fab-icon { font-size: 1.5rem; font-weight: 900; }
.fab-text { font-size: 1rem; letter-spacing: 0.5px; }

/* ==========================================================================
   SIDEBAR
   ========================================================================== */
.sidebar-card {
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  padding: 30px;
  border-radius: 20px;
  margin-bottom: 25px;
}

.tip-card {
  background: var(--color-primary-light);
  border-color: var(--color-primary-border);
}

.sidebar-card h3 { font-size: 1.1rem; font-weight: 800; margin-bottom: 10px; color: var(--text-primary); }
.sidebar-card p { font-size: 0.95rem; line-height: 1.6; color: var(--text-secondary); }

.side-nav { display: flex; flex-direction: column; gap: 8px; margin-top: 15px; }
.nav-item {
  display: flex;
  align-items: center;
  gap: 12px;
  text-decoration: none;
  color: var(--text-primary);
  padding: 14px;
  border-radius: 12px;
  font-weight: 700;
  font-size: 0.95rem;
  transition: all 0.2s;
}

.nav-item:hover {
  background: var(--color-primary-light);
  color: var(--color-primary);
}

/* ==========================================================================
   RESPONSIVE
   ========================================================================== */
@media (max-width: 1000px) {
  .dashboard-grid { grid-template-columns: 1fr; }
  .dashboard-sidebar { display: grid; grid-template-columns: 1fr 1fr; gap: 20px; }
}

@media (max-width: 768px) {
  .hero-body { flex-direction: column; text-align: center; }
  .hero-date-block { border-right: none; padding-right: 0; padding-bottom: 25px; border-bottom: 1px solid var(--border-default); width: 100%; }
  .dashboard-sidebar { grid-template-columns: 1fr; }
  .dashboard-header { padding: 40px 0; }
  .fab-text { display: none; }
  .fab-btn { padding: 15px; border-radius: 50%; }
}

/* Status Tags Sync */
.status-tag {
  display: inline-flex;
  align-items: center;
  padding: 6px 14px;
  border-radius: 8px;
  font-size: 0.75rem;
  font-weight: 800;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.status-confirmed { background: var(--color-primary-light); color: var(--color-primary); }
.status-scheduled { background: #fef3c7; color: #92400e; }
.status-cancelled { background: #fee2e2; color: #b91c1c; }
.status-completed { background: var(--color-gray-100); color: var(--text-muted); }
</style>