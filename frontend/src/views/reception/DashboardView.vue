<template>
  <div class="reception-container">
    <div class="page-header">
      <div class="header-content">
        <h1>Welcome back, <span>{{ auth.name }}</span></h1>
        <p>Active coordination of patient flow and clinic scheduling.</p>
      </div>
      <div class="header-actions">
        <router-link to="/reception/appointments" class="btn-primary-custom">+ New Booking</router-link>
      </div>
    </div>

    <div v-if="loading" class="loading-state empty-state">
      <Skeleton height="300px" width="100%" />
      <p>Analyzing clinic data...</p>
    </div>

    <div v-else class="dashboard-charts-layout animate-in">
      <!-- Pie Chart: Today's Breakdown -->
      <div class="chart-card shadow-card">
        <div class="chart-header-info">
          <h3 class="chart-title">Today's Appointment Breakdown</h3>
          <p class="chart-subtitle">Confirmed vs Unconfirmed vs Checked-In</p>
        </div>
        <div class="chart-container-inner">
          <DashboardChart
            :data="pieChartData"
            :options="chartOptions"
          />
        </div>
      </div>

      <!-- Bar Chart: Weekly Appointments -->
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
          <span class="summary-label">Today's Load</span>
          <span class="summary-value">{{ stats.today?.total || 0 }} </span>
          <p class="summary-desc">Total number of appointments scheduled for today.</p>
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
import Skeleton from "@/components/Skeleton.vue";
import DashboardChart from "@/components/admin/PieChart.vue";
import BarChart from "@/components/admin/BarChart.vue";
import { useAuthStore } from '@/stores/auth'

const auth = useAuthStore();
const loading = ref(true);
const stats = ref({
  doctors: 0,
  receptionists: 0,
  patients: 0,
  today: { total: 0, confirmed: 0, unconfirmed: 0, checkedIn: 0 }
});
const weeklyChartData = ref(null);

const pieChartData = computed(() => ({
  labels: ['Confirmed', 'Unconfirmed', 'Checked-In'],
  datasets: [
    {
      data: [
        stats.value.today?.confirmed || 0,
        stats.value.today?.unconfirmed || 0,
        stats.value.today?.checkedIn || 0
      ],
      backgroundColor: ['#4CAF50', '#FF9800', '#2196F3']
    }
  ]
}));

const chartOptions = {
  responsive: true,
  maintainAspectRatio: false
};


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
    const statsRes = await axios.get("/api/receptionist/dashboard/stats", { 
      headers: { Authorization: `Bearer ${auth.token}` } 
    });
    stats.value = statsRes.data;

    const weeklyRes = await axios.get("/api/receptionist/dashboard/weekly-stats", { 
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
  } catch (err) {
    console.error("Dashboard failed to load:", err);
  } finally {
    loading.value = false;
  }
};

onMounted(loadDashboard);
</script>

<style scoped>
.reception-container {
  max-width: 100vw;
  overflow-x: hidden;
}

.dashboard-charts-layout {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(min(100%, 400px), 1fr));
  gap: 25px;
  margin-top: 20px;
}

.chart-card {
  background: var(--bg-card);
  padding: 25px;
  border-radius: 0;
  border: 1px solid var(--border-default);
  display: flex;
  flex-direction: column;
  gap: 20px;
  width: 100%;
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
  min-height: 300px;
}

.summary-section {
  background: var(--bg-card);
  padding: 30px;
  border-radius: 0;
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

.btn-primary-custom {
  background: var(--color-primary);
  color: white;
  padding: 10px 20px;
  border-radius: 0;
  font-weight: 700;
  text-decoration: none;
  display: inline-block;
}

.animate-in {
  animation: fadeIn 0.8s ease-out forwards;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.empty-state {
  padding: 80px;
  text-align: center;
  color: var(--text-tertiary);
  font-style: italic;
}

@media (max-width: 768px) {
  .dashboard-charts-layout {
    grid-template-columns: 1fr;
  }
  .chart-card, .summary-section {
    padding: 20px;
  }
}
</style>