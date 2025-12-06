<script setup>
import axios from 'axios'
import { ref, onMounted } from 'vue'
import { useAuthStore } from '@/stores/auth' // ⬅️ NEW

const auth = useAuthStore() // ⬅️ NEW
const upcoming = ref([])

onMounted(async () => {
  try {
    // Note: While this still uses localStorage, it's inside the setup/mounted hook, 
    // where the browser environment is safer than the template.
    const res = await axios.get("http://localhost:5150/api/patient/upcoming", {
      headers: { Authorization: "Bearer " + localStorage.getItem("token") }
    })
    upcoming.value = res.data
  } catch (err) {
    console.error("Failed to fetch upcoming appointments:", err)
  }
})
</script>

<template>
  <h1>Welcome, {{ auth.name }}</h1>
  <h2>Upcoming Appointments</h2>

  <div v-if="upcoming.length === 0">
    No appointments scheduled.
  </div>

  <div v-for="app in upcoming" :key="app.id" class="card">
    <p><strong>Doctor:</strong> {{ app.doctorName }}</p>
    <p><strong>Date:</strong> {{ app.date }}</p>
    <p><strong>Time:</strong> {{ app.time }}</p>
  </div>
</template>

<style scoped>
.card {
  padding: 15px;
  background: white;
  border-radius: 8px;
  margin-bottom: 10px;
}
</style>