<script setup>
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const auth = useAuthStore()
const showMenu = ref(false)

onMounted(() => auth.loadFromStorage())

// Role-based links
const roleLinks = computed(() => {
  switch (auth.role) {
    case 'patient':
      return [
        { name: 'Dashboard', path: '/patient/dashboard' },
        { name: 'Appointments', path: '/patient/appointments' },
        { name: 'Notifications', path: '/patient/notifications' },
        { name: 'Profile', path: '/patient/profile' }, // This is the link we want in the dropdown
      ]
    case 'doctor':
      return [
        { name: 'Dashboard', path: '/doctor/dashboard' },
        { name: 'Appointments', path: '/doctor/appointments' },
        { name: 'Profile', path: '/doctor/profile' },
      ]
    case 'receptionist':
      return [
        { name: 'Dashboard', path: '/reception/dashboard' },
        { name: 'Appointments', path: '/reception/appointments' },
        { name: 'Profile', path: '/reception/profile' },
      ]
    case 'admin':
      return [
        { name: 'Dashboard', path: '/admin/dashboard' },
        { name: 'Users', path: '/admin/users' },
        { name: 'Logs', path: '/admin/logs' },
      ]
    default:
      return []
  }
})

// ✅ FIX: Safely retrieve the single Profile link object for the dropdown.
const profileLink = computed(() => {
  return roleLinks.value.find(link => link.name === 'Profile')
})

function toggleMenu() { showMenu.value = !showMenu.value }
</script>

<template>
<header class="header">
  <div class="header-inner">
    <router-link to="/" class="logo">Hospital<span>Booking</span>App</router-link>

    <nav :class="['nav', { open: showMenu }]">
      <router-link to="/doctors">Doctors</router-link>

      <template v-if="auth.isLoggedIn">
        <template v-for="link in roleLinks" :key="link.path">
          <router-link v-if="link.name !== 'Profile'" :to="link.path">{{ link.name }}</router-link>
        </template>

        <div class="dropdown" v-if="profileLink">
          <button class="dropbtn">Profile ▼</button>
          <div class="dropdown-content">
            <router-link :to="profileLink.path">Settings</router-link>
            <a @click="auth.logout(); router.push('/login')">Logout</a>
          </div>
        </div>
      </template>

      <template v-else>
        <router-link to="/login">Login</router-link>
        <router-link to="/register">Register</router-link>
      </template>
    </nav>

    <button class="menu-btn" @click="toggleMenu">☰</button>
  </div>
</header>
</template>


<style scoped>
.header {
  position: fixed;
  top: 0;
  left: 0;
  right: 0;
  height: 60px;
  background: #fff;
  border-bottom: 1px solid #ccc;
}
.header-inner {
  max-width: 1200px;
  width: 90%;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  height: 100%;
}
.logo {
  font-weight: bold;
  font-size: 1.4em;
  color: #000;
  text-decoration: none;
}
.logo span {
  font-weight: normal;
}
.nav {
  display: flex;
  gap: 20px;
  align-items: center;
}
.nav a {
  text-decoration: none;
  color: #000;
  font-size: 0.95em;
}
.nav a:hover {
  text-decoration: underline;
}
.dropdown {
  position: relative;
}
.dropbtn {
  background: none;
  border: none;
  cursor: pointer;
  font-size: 0.95em;
}
.dropdown-content {
  display: none;
  position: absolute;
  right: 0;
  top: 100%;
  background: #fff;
  border: 1px solid #ccc;
  min-width: 140px;
  box-shadow: 0 2px 5px rgba(0,0,0,0.2);
  flex-direction: column;
  z-index: 10;
}
.dropdown-content a {
  display: block;
  padding: 10px 15px;
  text-decoration: none;
  color: #000;
}
.dropdown-content a:hover {
  background-color: #f3f3f3;
}
.dropdown:hover .dropdown-content {
  display: flex;
  flex-direction: column;
}
.menu-btn {
  display: none;
  background: none;
  border: none;
  font-size: 1.5em;
  cursor: pointer;
}
@media (max-width: 768px) {
  .nav {
    display: none;
    flex-direction: column;
    gap: 10px;
    position: absolute;
    top: 60px;
    right: 0;
    width: 200px;
    padding: 10px;
    border: 1px solid #ccc;
    background: #fff;
  }
  .nav.open {
    display: flex;
  }
  .menu-btn {
    display: block;
  }
  .dropdown:hover .dropdown-content {
    display: none;
  }
}
</style>