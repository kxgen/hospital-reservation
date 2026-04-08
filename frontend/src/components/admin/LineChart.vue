<template>
  <div class="chart-wrapper">
    <div class="chart-container">
      <Line
        v-if="chartData.labels.length > 0"
        :data="chartData"
        :options="chartOptions"
      />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import {
  Chart as ChartJS,
  Title,
  Tooltip,
  Legend,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  Filler
} from 'chart.js'
import { Line } from 'vue-chartjs'

ChartJS.register(
  Title,
  Tooltip,
  Legend,
  LineElement,
  PointElement,
  CategoryScale,
  LinearScale,
  Filler
)

const props = defineProps({
  chartData: {
    type: Object,
    required: true
  }
})

const chartOptions = computed(() => ({
  responsive: true,
  maintainAspectRatio: false,
  resizeDelay: 100,
  plugins: {
    legend: {
      display: false // We'll use a custom title in the dashboard
    },
    tooltip: {
      mode: 'index',
      intersect: false,
      backgroundColor: 'rgba(33, 37, 41, 0.9)',
      titleFont: { size: 13, weight: 'bold' },
      bodyFont: { size: 12 },
      padding: 12,
      cornerRadius: 0,
      displayColors: false
    }
  },
  scales: {
    y: {
      beginAtZero: true,
      grid: {
        color: 'rgba(0, 0, 0, 0.05)',
        drawBorder: false
      },
      ticks: {
        stepSize: 1,
        font: { size: 11 }
      }
    },
    x: {
      grid: {
        display: false
      },
      ticks: {
        font: { size: 11 },
        maxRotation: 45,
        minRotation: 0
      }
    }
  },
  elements: {
    line: {
      tension: 0.4, // Smooth curves
      borderWidth: 3,
      borderColor: '#E91E63',
      fill: true,
      backgroundColor: (context) => {
        const chart = context.chart
        const { ctx, chartArea } = chart
        if (!chartArea) return null
        const gradient = ctx.createLinearGradient(0, chartArea.top, 0, chartArea.bottom)
        gradient.addColorStop(0, 'rgba(233, 30, 99, 0.2)')
        gradient.addColorStop(1, 'rgba(233, 30, 99, 0)')
        return gradient
      }
    },
    point: {
      radius: 4,
      hoverRadius: 6,
      backgroundColor: '#E91E63',
      borderWidth: 2,
      borderColor: '#fff'
    }
  }
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
}

.chart-container canvas {
  max-width: 100% !important;
}
</style>
