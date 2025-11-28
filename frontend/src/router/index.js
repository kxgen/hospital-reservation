// src/router/index.js
import { createRouter, createWebHistory } from 'vue-router'

import Login from '@/views/LoginView.vue'
import Register from '@/views/RegisterView.vue'
import Home from '@/views/HomeView.vue'
import DoctorSearch from '@/views/SearchView.vue'
import BookAppointment from '@/views/BookAppointmentView.vue'
import Doctor from '@/views/DoctorView.vue'
import Patient from '@/views/PatientView.vue'

const routes = [
  { path: '/', name: 'home', component: Home },
  { path: '/login', name: 'login', component: Login },
  { path: '/register', name: 'register', component: Register},
  { path: '/doctor-search', name: 'doctorsearch', component: DoctorSearch},
  { path: '/doctor', name: 'doctor', component: Doctor},
  { path: '/book-appointment', name: 'bookappointment', component: BookAppointment},
  { path: '/patient', name: 'patient', component: Patient}
]

const router = createRouter({
  history: createWebHistory(),
  routes
})

export default router
