<script setup>
import axios from 'axios'
import { ref, onMounted } from 'vue'

const user = ref({})

onMounted(async () => {
  try {
    const res = await axios.get("http://localhost:5150/api/patient/profile", {
      headers: { Authorization: "Bearer " + localStorage.getItem("token") }
    })
    user.value = res.data
  } catch (err) {
    console.error("Failed to fetch profile:", err)
  }
})
</script>

<template>
  <h1>My Profile</h1>

  <div class="card">
    <p><strong>Name:</strong> {{ user.firstName }} {{ user.lastName }}</p>
    <p><strong>Email:</strong> {{ user.email }}</p>
    <p><strong>Phone:</strong> {{ user.phone }}</p>
    <p><strong>Date of Birth:</strong> {{ user.dateOfBirth }}</p>
    <p><strong>Gender:</strong> {{ user.gender }}</p>
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
