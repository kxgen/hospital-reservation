import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'


import Home from '@/views/public/HomeView.vue'
import Login from '@/views/public/LoginView.vue'
import Register from '@/views/public/RegisterView.vue'
import DoctorsList from '@/views/public/DoctorsListView.vue'
import DoctorOverview from '@/views/public/DoctorOverviewView.vue'
import Support from '@/views/public/SupportView.vue'

const routes = [
  
  
  
  { path: '/', name: 'home', component: Home },
  { path: '/login', name: 'login', component: Login },
  { path: '/register', name: 'register', component: Register },
  { path: '/forgot-password', name: 'forgot-password', component: () => import('@/views/public/ForgotPasswordView.vue') },
  { path: '/support', name: 'support', component: Support },
  {
    path: '/force-password-change',
    name: 'force-password-change',
    component: () => import('@/views/public/ForcePasswordChangeView.vue'),
    meta: { requiresAuth: true }
  },

  
  { path: '/doctors', name: 'doctors', component: DoctorsList },
  {
    path: '/doctors/:id',
    name: 'doctor-overview',
    component: DoctorOverview,
    props: true,
  },

  
  
  
  {
    path: '/patient',
    component: () => import('@/views/patient/PatientLayout.vue'),
    meta: { requiresAuth: true, roles: ['patient'] },
    children: [
      {
        path: 'dashboard',
        name: 'patient-dashboard',
        component: () => import('@/views/patient/PatientDashboardView.vue'),
      },
      {
        path: 'appointments',
        name: 'patient-appointments',
        component: () => import('@/views/patient/PatientAppointmentsView.vue'),
      },
      {
        path: 'edit-appointment/:id',
        name: 'patient-edit-appointment',
        component: () => import('@/views/patient/PatientEditAppointmentView.vue'),
        props: true,
      },
      {
        path: 'notifications',
        name: 'patient-notifications',
        component: () => import('@/views/patient/PatientNotificationsView.vue'),
        meta: { title: 'Health Center - Trinity Specialized Center' }
      },
      {
        path: 'book-appointment',
        name: 'book-appointment',
        component: () => import('@/views/patient/BookView.vue'),
      },
      {
        path: 'profile',
        name: 'patient-profile',
        component: () => import('@/views/patient/PatientProfileView.vue'),
      },
    ],
  },

  
  
  
  {
    path: '/doctor',
    component: () => import('@/views/doctor/Layout.vue'),
    meta: { requiresAuth: true, roles: ['doctor'] },
    children: [
      {
        path: 'dashboard',
        name: 'doctor-dashboard',
        component: () => import('@/views/doctor/DoctorDashboardView.vue'),
      },
      {
        path: 'appointments',
        name: 'doctor-appointments',
        component: () => import('@/views/doctor/DoctorAppointmentsView.vue'),
      },
      
      {
        path: 'appointments/:id/session',
        name: 'appointment-session',
        component: () => import('@/views/doctor/DoctorSessionFormView.vue'),
        props: true,
      },
      {
        path: 'appointments/:id',
        name: 'doctor-appointment-detail',
        component: () => import('@/views/doctor/AppointmentDetailView.vue'),
        props: true,
      },
      {
        path: 'profile',
        name: 'doctor-profile',
        component: () => import('@/views/doctor/DoctorProfileView.vue'),
      },
      {
        path: 'schedule-followup',
        name: 'doctor-schedule-followup',
        component: () => import('@/views/doctor/DoctorSessionFormView.vue'),
      },
    ],
  },

  
  
  
  {
    path: '/reception',
    component: () => import('@/views/reception/Layout.vue'),
    meta: { requiresAuth: true, roles: ['receptionist'] },
    children: [
      {
        path: 'dashboard',
        name: 'reception-dashboard',
        component: () => import('@/views/reception/DashboardView.vue'),
      },
      {
        path: 'appointments',
        name: 'reception-appointments',
        component: () => import('@/views/reception/AppointmentsPortalView.vue'),
      },
      {
        path: 'book-form',
        name: 'reception-book-form',
        component: () => import('@/views/reception/BookFormView.vue'),
      },
      {
        path: 'manage-appointments',
        name: 'reception-manage-appointments',
        component: () => import('@/views/reception/ManageAppointmentsView.vue'),
      },
      {
        path: 'patient-contact',
        name: 'reception-patient-contact',
        component: () => import('@/views/reception/PatientContactView.vue'),
      },
      {
        path: 'unavailability',
        name: 'reception-unavailability',
        component: () => import('@/views/reception/ManageUnavailabilityView.vue'),
      },
      {
        path: 'profile',
        name: 'reception-profile',
        component: () => import('@/views/reception/ProfileView.vue'),
      },
    ],
  },

  
  
  
  {
    path: '/admin',
    component: () => import('@/views/admin/Layout.vue'),
    meta: { requiresAuth: true, roles: ['admin'] },
    children: [
      {
        path: 'dashboard',
        name: 'admin-dashboard',
        component: () => import('@/views/admin/DashboardView.vue'),
      },
      {
        path: 'accounts',
        name: 'admin-accounts',
        component: () => import('@/views/admin/AccountManagementView.vue'),
      },
      {
        path: 'accounts/:id/manage',
        name: 'admin-account-manage',
        component: () => import('@/views/admin/UserDetailManagementView.vue'),
        props: (route) => ({ id: route.params.id, role: route.query.role }),
      },
      {
        path: 'staff/add',
        name: 'admin-staff-add',
        component: () => import('@/views/admin/CreateStaffMemberView.vue'),
      },
      { path: 'logs', name: 'admin-logs', component: () => import('@/views/admin/LogsView.vue') },
      {
        path: 'profile',
        name: 'admin-profile',
        component: () => import('@/views/admin/ProfileView.vue'),
      },
      {
        path: 'doctors/:id/availability', 
        name: 'admin-doctor-availability',
        component: () => import('@/views/admin/ConfigureScheduleView.vue'),
        props: true,
      },
    ],
  },
]

const router = createRouter({
  history: createWebHistory(),
  routes,
  scrollBehavior(to, from, savedPosition) {
    
    
    if (savedPosition) {
      return savedPosition
    } else {
      return { top: 0 }
    }
  },
})


router.beforeEach(async (to) => {
  const auth = useAuthStore()

  
  if (!auth.isLoaded) {
    await auth.loadFromStorage()
  }

  const requiresAuth = to.matched.some((record) => record.meta.requiresAuth)
  const allowedRoles = to.meta.roles || []

  
  if (requiresAuth && !auth.isLoggedIn) {
    return { name: 'login' }
  }

  
  if (requiresAuth && allowedRoles.length && !allowedRoles.includes(auth.role)) {
    return { name: 'home' }
  }

  
  const isPassChangeReq = auth.isPasswordChangeRequired || localStorage.getItem('isPasswordChangeRequired') === 'true'
  if (auth.isLoggedIn && isPassChangeReq && to.name !== 'force-password-change') {
    return { name: 'force-password-change' }
  }

  
  if (auth.isLoggedIn && !isPassChangeReq && to.name === 'force-password-change') {
    return { name: 'home' }
  }

  return true
})

export default router
