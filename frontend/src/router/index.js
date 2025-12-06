// src/router/index.js
import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth' // ⬅️ IMPORT THE AUTH STORE

// Public Views (Keep these imports)
import Home from '@/views/public/HomeView.vue'
import Login from '@/views/public/LoginView.vue'
import Register from '@/views/public/RegisterView.vue'
import DoctorsList from '@/views/public/DoctorsListView.vue'
import DoctorProfile from '@/views/public/DoctorProfileView.vue'
import BookAppointment from '@/views/public/BookAppointmentView.vue'

// ❌ REMOVE THE requireRole FUNCTION. It is replaced by the global guard below.

const routes = [
  // PUBLIC
  { path: '/', name: 'home', component: Home },
  { path: '/login', name: 'login', component: Login },
  { path: '/register', name: 'register', component: Register },
  { path: '/doctors', name: 'doctors', component: DoctorsList },
  { path: '/doctor/:id', name: 'doctor-profile', component: DoctorProfile },
  { path: '/book-appointment/:doctorId', name: 'book-appointment', component: BookAppointment },

  // PATIENT
  {
    path: '/patient',
    component: () => import('@/views/patient/Layout.vue'),
    // 🔑 NEW: Use meta fields to specify required role
    meta: { requiresAuth: true, roles: ['patient'] }, 
    children: [
      { path: 'dashboard', name: 'patient-dashboard', component: () => import('@/views/patient/DashboardView.vue') },
      { path: 'appointments', name: 'patient-appointments', component: () => import('@/views/patient/AppointmentsView.vue') },
      { path: 'notifications', name: 'patient-notifications', component: () => import('@/views/patient/NotificationsView.vue') },
      { path: 'profile', name: 'patient-profile', component: () => import('@/views/patient/ProfileView.vue') },
    ],
  },

  // DOCTOR
  {
    path: '/doctor',
    component: () => import('@/views/doctor/Layout.vue'),
    // 🔑 NEW: Use meta fields
    meta: { requiresAuth: true, roles: ['doctor'] },
    children: [
      { path: 'dashboard', name: 'doctor-dashboard', component: () => import('@/views/doctor/DashboardView.vue') },
      { path: 'appointments', name: 'doctor-appointments', component: () => import('@/views/doctor/AppointmentsView.vue') },
      { path: 'profile', name: 'doctor-profile', component: () => import('@/views/doctor/ProfileView.vue') },
    ],
  },

  // RECEPTIONIST
  {
    path: '/reception',
    component: () => import('@/views/reception/Layout.vue'),
    // 🔑 NEW: Use meta fields
    meta: { requiresAuth: true, roles: ['receptionist'] },
    children: [
      { path: 'dashboard', name: 'reception-dashboard', component: () => import('@/views/reception/DashboardView.vue') },
      { path: 'appointments', name: 'reception-appointments', component: () => import('@/views/reception/AppointmentsView.vue') },
      { path: 'profile', name: 'reception-profile', component: () => import('@/views/reception/ProfileView.vue') },
    ],
  },

  // ADMIN
  {
    path: '/admin',
    component: () => import('@/views/admin/Layout.vue'),
    // 🔑 NEW: Use meta fields
    meta: { requiresAuth: true, roles: ['admin'] },
    children: [
      { path: 'dashboard', name: 'admin-dashboard', component: () => import('@/views/admin/DashboardView.vue') },
      { path: 'users', name: 'admin-users', component: () => import('@/views/admin/UsersView.vue') },
      { path: 'logs', name: 'admin-logs', component: () => import('@/views/admin/LogsView.vue') },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
})

// 🚀 THE FIX: Global Navigation Guard
router.beforeEach(async (to) => {
  // Use the store instance
  const auth = useAuthStore()

  // 1. Await loading of the state if it hasn't happened yet.
  // This pauses navigation until we know the role.
  if (!auth.isLoaded) {
    await auth.loadFromStorage() 
  }
  
  const requiresAuth = to.matched.some(record => record.meta.requiresAuth)
  const allowedRoles = to.meta.roles || []

  // 2. Handle unauthorized access
  if (requiresAuth && !auth.isLoggedIn) {
    // User is not logged in but tries to access a protected route
    return { name: 'login' }
  }

  // 3. Handle incorrect role access
  if (requiresAuth && allowedRoles.length && !allowedRoles.includes(auth.role)) {
    // User is logged in but does not have the correct role for this route
    // Redirect to the home page or a specific unauthorized page
    return { path: '/' }
  }

  // 4. Everything is fine, proceed
  return true
})

export default router