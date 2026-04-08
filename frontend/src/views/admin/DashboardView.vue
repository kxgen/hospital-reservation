<template>
  <div class="admin-container">
    <div class="page-header">
      <div class="header-content">
        <h1>Welcome back, <span>{{ auth.name }}</span></h1>
        <p>Here is an overview of clinic operations and personnel.</p>
      </div>
    </div>

    <div v-if="loading" class="loading-state empty-state">
      <Skeleton height="300px" width="100%" />
      <p>Analyzing clinic data...</p>
    </div>

    <div v-else class="dashboard-charts-layout animate-in">
      <div class="chart-card shadow-card">
        <div class="chart-header-info">
          <h3 class="chart-title">Users</h3>
          <p class="chart-subtitle">Breakdown of system users by role</p>
        </div>
        <div class="chart-container-inner">
          <DashboardChart
            :data="chartData"
            :options="chartOptions"
          />
        </div>
      </div>

      <div class="chart-card shadow-card">
        <div class="chart-header-info">
          <h3 class="chart-title">Registration Growth</h3>
          <p class="chart-subtitle">User registration trends over the last 30 days</p>
        </div>
        <div class="chart-container-inner">
          <LineChart
            v-if="registrationChartData"
            :chart-data="registrationChartData"
          />
        </div>
      </div>

      <div class="chart-card shadow-card">
        <div class="chart-header-info">
          <h3 class="chart-title">This Week's Appointments</h3>
          <p class="chart-subtitle">Appointment volume over this week.</p>
        </div>
        <div class="chart-container-inner">
          <BarChart
            v-if="weeklyChartData"
            :data="weeklyChartData"
            :options="chartOptions"
          />
        </div>
      </div>
    </div>

    <!-- Key Insights Summary -->
    <section v-if="!loading" class="summary-section shadow-card animate-in" style="animation-delay: 0.2s">
      <div class="summary-header">
        <h3 class="summary-title">Clinic Insights</h3>
        <p class="summary-subtitle">Automated summary of current system metrics</p>
      </div>
      
      <div class="summary-grid">
        <div class="summary-item">
          <span class="summary-label">Total Personnel</span>
          <span class="summary-value">{{ totalUsers }} Users</span>
          <p class="summary-desc">Combined count of active doctors, receptionists, and registered patients.</p>
        </div>
        <div class="summary-item">
          <span class="summary-label">Recent Growth</span>
          <span class="summary-value">+{{ recentRegistrations }} </span>
          <p class="summary-desc">New user accounts created within the last month.</p>
        </div>
        <div class="summary-item">
          <span class="summary-label">Peak Activity</span>
          <span class="summary-value">{{ peakDay.day || 'N/A' }}</span>
          <p class="summary-desc">The most active day of the current week with {{ peakDay.count || 0 }} scheduled appointments.</p>
        </div>
      </div>
    </section>

  </div>
</template>

<script setup>
import { ref, onMounted, computed } from "vue";
import axios from "@/api/axios";
import { formatDate, formatTime } from "@/utils/dateFormatter";
import Skeleton from "@/components/Skeleton.vue";
import DashboardChart from "@/components/admin/PieChart.vue";
import BarChart from "@/components/admin/BarChart.vue";
import LineChart from "@/components/admin/LineChart.vue";
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore();

const chartData = ref({
  labels: ['Patients', 'Doctors', 'Receptionists'],
  datasets: [
    {
      label: 'Users',
      data: [0, 0, 0],
      backgroundColor: [
        '#4CAF50', // Patients - green
        '#2196F3', // Doctors - blue
        '#FF9800'  // Receptionists - orange
      ]
    }
  ]
});

const weeklyChartData = ref(null);
const registrationChartData = ref(null);

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false
};

const stats = ref({
  doctors: 0,
  receptionists: 0,
  patients: 0,
  appointmentsToday: 0,
});

const logs = ref([]);
const loading = ref(true);

const totalUsers = computed(() => {
  return (stats.value.doctors || 0) + (stats.value.receptionists || 0) + (stats.value.patients || 0);
});

const recentRegistrations = computed(() => {
  if (!registrationChartData.value) return 0;
  return registrationChartData.value.datasets[0].data.reduce((acc, curr) => acc + curr, 0);
});

const peakDay = computed(() => {
  if (!weeklyChartData.value) return { day: '', count: 0 };
  const data = weeklyChartData.value.datasets[0].data;
  const labels = weeklyChartData.value.labels;
  const max = Math.max(...data);
  const index = data.indexOf(max);
  return { day: labels[index], count: max };
});

const loadDashboard = async () => {
  try {
    loading.value = true;
    const statsRes = await axios.get("/api/admin/dashboard/stats", { 
      headers: { Authorization: `Bearer ${auth.token}` } 
    });
    stats.value = statsRes.data;

    chartData.value = {
      labels: ['Patients', 'Doctors', 'Receptionists'],
      datasets: [
        {
          data: [
            stats.value.patients,
            stats.value.doctors,
            stats.value.receptionists
          ],
          backgroundColor: ['#9C27B0', '#00BCD4', '#FFC107']
        }
      ]
    }

    const weeklyRes = await axios.get("/api/admin/dashboard/weekly-stats", { 
      headers: { Authorization: `Bearer ${auth.token}` } 
    });
    
    const daysArr = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    const todayUTC = daysArr[new Date().getUTCDay()];

    weeklyChartData.value = {
      labels: weeklyRes.data.map(d => d.day),
      datasets: [{
        label: 'Weekly Appointments',
        data: weeklyRes.data.map(d => d.count),
        backgroundColor: weeklyRes.data.map(d => d.day === todayUTC ? '#FF5722' : '#3F51B5'),
        borderRadius: 6
      }]
    };

    const regRes = await axios.get("/api/admin/dashboard/registration-stats", { 
      headers: { Authorization: `Bearer ${auth.token}` } 
    });

    registrationChartData.value = {
      labels: regRes.data.map(d => d.date),
      datasets: [{
        label: 'New Registrations',
        data: regRes.data.map(d => d.count),
        borderColor: '#E91E63',
        backgroundColor: 'rgba(233, 30, 99, 0.1)',
        fill: true,
        tension: 0.4
      }]
    };

    const logsRes = await axios.get("/api/admin/dashboard/logs", { 
      headers: { Authorization: `Bearer ${auth.token}` } 
    });
    logs.value = Array.isArray(logsRes.data) ? logsRes.data.slice(0, 5) : [];
  } catch (err) {
    console.error("Dashboard failed to load:", err);
  } finally {
    loading.value = false;
  }
};

onMounted(loadDashboard);
</script>

<style scoped>
.admin-container {
  max-width: 100vw;
  overflow-x: hidden;
}

.dashboard-charts-layout {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 280px), 1fr));
  gap: 25px;
  margin-top: 20px;
}

.chart-card {
  background: var(--bg-card);
  padding: 25px;
  border-radius: 12px;
  border: 1px solid var(--border-default);
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
}

.summary-section {
  background: var(--bg-card);
  padding: 30px;
  border-radius: 12px;
  border: 1px solid var(--border-default);
  margin-top: 30px;
}

.summary-header {
  margin-bottom: 25px;
  border-left: 4px solid var(--color-primary);
  padding-left: 15px;
}

.summary-title {
  font-size: 1.4rem;
  font-weight: 800;
  color: var(--text-primary);
  margin: 0;
}

.summary-subtitle {
  font-size: 0.9rem;
  color: var(--text-tertiary);
  margin: 5px 0 0 0;
}

.summary-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(240px, 1fr));
  gap: 30px;
}

.summary-item {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.summary-label {
  font-size: 0.8rem;
  font-weight: 700;
  color: var(--text-tertiary);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.summary-value {
  font-size: 1.8rem;
  font-weight: 900;
  color: var(--color-primary);
}

.summary-desc {
  font-size: 0.85rem;
  color: var(--text-secondary);
  line-height: 1.4;
  margin: 0;
}

.spacer-top { margin-top: 30px; }

.animate-in {
  animation: fadeIn 0.8s ease-out forwards;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.chart-header-info {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.chart-title {
  font-size: 1.15rem;
  font-weight: 700;
  color: var(--text-primary);
  margin: 0;
}

.chart-subtitle {
  font-size: 0.8rem;
  color: var(--text-secondary);
  margin: 0;
}

.chart-container-inner {
  position: relative;
  width: 100%;
  /* Removed overflow: hidden to prevent clipping tooltips/labels */
}

@media (max-width: 1100px) {
  .dashboard-charts-layout {
    grid-template-columns: repeat(auto-fit, minmax(min(100%, 280px), 1fr));
  }
}

@media (max-width: 768px) {
  .dashboard-charts-layout {
    padding: 0px;
  }
  .chart-card {
    padding: 20px;
  }
  .summary-section {
    padding: 20px;
  }
  .chart-container-inner {
    min-height: 280px;
  }
}

.time-block { display: flex; flex-direction: column; }
.time-block .date { font-weight: 800; color: var(--text-primary); font-size: 0.95rem; }
.time-block .time { font-size: 0.75rem; color: var(--text-secondary); font-weight: 600; }

.action-name { display: block; font-weight: 700; color: var(--text-primary); margin-bottom: 2px; }
.action-detail { font-size: 0.8rem; color: var(--text-tertiary); margin: 0; }

.user-chip {
  padding: 5px 12px;
  background: var(--color-primary-light);
  color: var(--color-primary);
  border-radius: 0;
  font-weight: 700;
  font-size: 0.8rem;
  border: 1px solid var(--color-primary-border);
}

.empty-state {
  padding: 80px;
  text-align: center;
  color: var(--text-tertiary);
  font-style: italic;
}

.text-link {
  color: var(--color-primary);
  font-weight: 800;
  text-decoration: none;
  font-size: 0.9rem;
}
.text-link:hover { text-decoration: underline; }
</style>