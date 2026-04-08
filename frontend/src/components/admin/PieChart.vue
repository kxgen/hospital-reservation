<template>
  <div class="chart-wrapper">
    <div class="chart-container">
      <Pie :data="data" :options="chartOptions" />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { Pie } from 'vue-chartjs'
import {
  Chart as ChartJS,
  ArcElement,
  Tooltip,
  Legend
} from 'chart.js'

ChartJS.register(ArcElement, Tooltip, Legend)

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

// Merge default responsive options with user-provided options
const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false, // Allows the chart to fill the container height
  resizeDelay: 100,
  plugins: {
    legend: {
      position: 'bottom', // Usually looks better on mobile
      labels: {
        padding: 20,
        usePointStyle: true,
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
  padding: 5px;
}

@media (max-width: 768px) {
  .chart-container {
    height: 280px;
  }
  .chart-wrapper {
    padding: 0.5rem;
  }
}
.chart-container canvas {
  max-width: 100% !important;
}
</style>