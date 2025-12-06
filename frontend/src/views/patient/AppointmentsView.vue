<script setup>
import axios from 'axios'
import { ref, onMounted } from 'vue'

const appointments = ref([])

onMounted(async () => {
  try {
    const res = await axios.get("http://localhost:5150/api/patient/appointments", {
      headers: { Authorization: "Bearer " + localStorage.getItem("token") }
    })
    appointments.value = res.data
  } catch (err) {
    console.error("Failed to fetch appointments:", err)
  }
})
</script>

<template>
  <h1>My Appointments</h1>

  <table class="table">
    <thead>
      <tr>
        <th>Doctor</th>
        <th>Date</th>
        <th>Time</th>
        <th>Status</th>
      </tr>
    </thead>
    <tbody>
      <tr v-for="a in appointments" :key="a.id">
        <td>{{ a.doctorName }}</td>
        <td>{{ a.date }}</td>
        <td>{{ a.time }}</td>
        <td>{{ a.status }}</td>
      </tr>
    </tbody>
  </table>
</template>

<style scoped>
.table {
  width: 100%;
  border-collapse: collapse;
}

.table th,
.table td {
  padding: 10px;
  border: 1px solid #ddd;
}

.table th {
  background-color: #f0f0f0;
  text-align: left;
}
</style>
