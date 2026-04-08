<template>
  <div class="chart-wrapper">
    <div class="chart-container">
      <Bar :data="data" :options="chartOptions" />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { Bar } from 'vue-chartjs'
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  BarElement,
  CategoryScale,
  LinearScale
} from 'chart.js'

ChartJS.register(Title, Tooltip, Legend, BarElement, CategoryScale, LinearScale)

const props = defineProps({
  data: {
    type: Object,
    required: true
  },
  options: {
    type: Object,
    default: () => ({})
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  resizeDelay: 100,
  plugins: {
    legend: {
      position: 'top',
      labels: {
        padding: 20,
        usePointStyle: true,
      }
    }
  },
  scales: {
    y: {
      beginAtZero: true,
      ticks: {
        stepSize: 1
      }
    }
  },
  ...props.options
}))
</script>

<style scoped>
.chart-wrapper {
  width: 100%;
  max-width: 100%;
  margin: 0 auto;
  padding: 1rem;
  will-change: transform, opacity;
}

.chart-container {
  position: relative;
  height: 360px; 
  min-height: 250px;
  padding: 5px 5px 30px 5px; /* Increased bottom padding to prevent label clipping */
}

@media (max-width: 768px) {
  .chart-container {
    height: 280px;
  }
}
.chart-container canvas {
  max-width: 100% !important;
}
</style>
