<script setup>
import { ref, onMounted, computed, watch } from 'vue'
import { useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Logo from '@/components/Logo.vue'

const router = useRouter()
const auth = useAuthStore()
const showMenu = ref(false)

let pollInterval = null

onMounted(async () => {
    await auth.loadFromStorage()
    await auth.fetchUnreadCount()
    pollInterval = setInterval(() => auth.fetchUnreadCount(), 30000) // Poll every 30s
})

import { onUnmounted } from 'vue'
onUnmounted(() => { if (pollInterval) clearInterval(pollInterval) })

watch(() => auth.isLoggedIn, (newVal) => {
  if (newVal) {
    auth.fetchUnreadCount()
  }
})

const roleLinks = computed(() => {
  if (!auth.role) return []
  switch (auth.role.toLowerCase()) {
    case 'patient':
      return [
        { name: 'Dashboard', path: '/patient/dashboard' },
        { name: 'Appointments', path: '/patient/appointments' },
        { name: 'Notifications', path: '/patient/notifications' },
        { name: 'Profile', path: '/patient/profile' },
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
        { name: 'CommCenter', path: '/reception/patient-contact' },
        { name: 'Profile', path: '/reception/profile' },
      ]
    case 'admin':
      return [
        { name: 'Dashboard', path: '/admin/dashboard' },
        { name: 'Accounts', path: '/admin/accounts' },
        { name: 'Logs', path: '/admin/logs' },
        { name: 'Profile', path: '/admin/profile' },
      ]
    default:
      return []
  }
})

// Used only for Desktop Dropdown
const profileLink = computed(() => {
  return roleLinks.value.find(link => link.name === 'Profile')
})

function toggleMenu() { showMenu.value = !showMenu.value }

const handleLogout = () => {
  showMenu.value = false
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <header class="header">
    <div class="header-inner">
      <router-link to="/" class="logo">
        <div class="logo-icon">
          <Logo />
        </div>
        <div class="logo-text">
          <span class="main">Trinity</span>
          <span class="suffix">Specialized Center</span>
        </div>
      </router-link>

      <nav :class="['nav', { open: showMenu }]">
        <template v-if="auth.isLoggedIn">
          <template v-for="link in roleLinks" :key="link.path">
            <router-link 
              v-if="link.name !== 'Profile'" 
              :to="link.path" 
              class="nav-link"
              @click="showMenu = false"
            >
              {{ link.name }}
              <span v-if="link.name === 'Notifications' && auth.unreadNotificationCount > 0" class="nav-indicator">
                {{ auth.unreadNotificationCount }}
              </span>
            </router-link>
          </template>

          <div class="dropdown desktop-only" v-if="profileLink">
            <button class="dropbtn">
              Profile <span class="chevron">▼</span>
            </button>
            <div class="dropdown-content">
              <router-link :to="profileLink.path">Settings</router-link>
              <a @click="handleLogout" class="logout-link">Logout</a>
            </div>
          </div>

          <template v-if="profileLink">
            <router-link 
              :to="profileLink.path" 
              class="nav-link mobile-only"
              @click="showMenu = false"
            >
              Settings
            </router-link>
            <a @click="handleLogout" class="nav-link mobile-only logout-mobile">
              Logout
            </a>
          </template>
        </template>

        <template v-else>
          <router-link to="/login" class="nav-link" @click="showMenu = false">Login</router-link>
          <router-link to="/register" class="btn-register" @click="showMenu = false">Register</router-link>
        </template>
      </nav>

    </div>
  </header>
</template>

<style scoped>
/* ==========================================================================
   DESKTOP STYLES
   ========================================================================== */
.header {
  position: fixed;
  top: 0; left: 0; right: 0;
  min-height: 4rem;
  height: auto;
  background: var(--bg-card);
  border-bottom: 1px solid var(--border-default);
  z-index: 1000;
  box-shadow: var(--shadow-sm);
}

.header-inner {
  max-width: 1400px;
  width: 90%;
  margin: 0 auto;
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 0;
  gap: 15px;
}

.logo { display: flex; align-items: center; gap: 8px; text-decoration: none;}
.logo-icon {
  padding: 0;
  width: 2rem; height: 2rem;
  border-radius: 10px;
  display: flex; align-items: center; justify-content: center; padding: 8px;
}
.logo-icon svg {
  display: block;
  width: 100%;
  height: 100%;
}

.logo-text .main { font-size: 1.4rem; font-weight: 800; color: var(--text-primary); display: block; }
.logo-text .suffix { font-size: 0.7rem; font-weight: 700; color: var(--color-primary); text-transform: uppercase; letter-spacing: 1.2px; display: block; }

.nav { display: flex; gap: 30px; align-items: center; }
.nav-link {
  text-decoration: none;
  color: var(--text-secondary);
  font-weight: 600;
  font-size: 0.95rem;
  position: relative;
  padding: 8px 0;
  transition: color 0.2s;
}

.nav-link:hover, .nav-link.router-link-exact-active { color: var(--color-primary); }
.nav-link.router-link-exact-active::after {
  content: ''; position: absolute; bottom: 0; left: 0; width: 100%; height: 3px;
  background-color: var(--color-primary); border-radius: 2px;
}

/* Visibility Helpers */
.mobile-only { display: none; }

/* Dropdown */
.dropdown { position: relative; height: 100%; display: flex; align-items: center; }
.dropbtn {
  background: var(--color-primary); color: var(--color-white);
  border: none; padding: 9px 18px; border-radius: 8px;
  font-weight: 700; cursor: pointer; display: flex; align-items: center; gap: 8px;
}
.dropdown-content {
  display: none; position: absolute; right: 0; top: calc(100% + 12px);
  background: var(--bg-card); border: 1px solid var(--border-default);
  min-width: 170px; border-radius: 12px; box-shadow: var(--shadow-md);
  flex-direction: column; padding: 6px 0; z-index: 1010;
}
.dropdown-content::before { content: ''; position: absolute; top: -15px; left: 0; right: 0; height: 15px; background: transparent; }
.dropdown:hover .dropdown-content { display: flex; }
.dropdown-content a { padding: 10px 18px; text-decoration: none; color: var(--text-primary); font-size: 0.9rem; font-weight: 500; cursor: pointer; }
.dropdown-content a:hover { background: var(--bg-input); color: var(--color-primary); }
.logout-link { color: var(--text-error, #a83232) !important; border-top: 1px solid var(--color-gray-50); margin-top: 4px; }

.btn-register { background: var(--color-primary); color: var(--color-white); padding: 9px 20px; border-radius: 8px; text-decoration: none; font-weight: 700; }
.menu-btn { display: none; background: none; border: none; font-size: 1.6rem; color: var(--color-primary); cursor: pointer; }

/* ==========================================================================
   MOBILE STYLES
   ========================================================================== */
@media (max-width: 850px) {
  .header-inner {
    flex-direction: column;
    padding: 15px 0;
    gap: 15px;
    align-items: center;
  }

  .menu-btn { display: block; }
  .desktop-only { display: none; }
  .mobile-only { display: block; }

  .nav {
    display: flex;
    position: static;
    width: 100%;
    background: transparent;
    flex-direction: column; /* Stack vertically for block feel */
    gap: 6px;
    padding: 10px 0;
    box-shadow: none;
    align-items: stretch; /* Full width */
    justify-content: flex-start;
    border-bottom: none;
  }

  .nav-link {
    display: flex;
    width: 100%;
    justify-content: center;
    padding: 10px 16px;
    border: 1px solid var(--border-default);
    background: var(--bg-card);
    border-radius: 6px;
    font-size: 0.85rem;
    font-weight: 700;
    color: var(--text-primary);
  }

  .btn-register {
    text-align: center;
    width: 100%;
  }

  .nav-link:hover {
    background: var(--bg-input);
    color: var(--color-primary);
  }

  .nav-link.router-link-exact-active {
    background: var(--color-primary);
    color: white;
    border-color: var(--color-primary);
  }
  
  .logout-mobile { 
    color: var(--text-error, #a83232) !important; 
    border-color: var(--text-error, #a83232);
  }

  .btn-register {
    width: auto;
    margin: 0;
    padding: 6px 12px;
    font-size: 0.8rem;
  }
}

/* Floating Indicator Badge */
.nav-indicator {
    position: absolute;
    top: -4px;              /* slightly above for visual balance */
    right: -10px;
    background: var(--color-primary);
    color: #fff;
    font-size: 0.65rem;
    font-weight: 800;
    border-radius: 999px;
    min-width: 18px;
    height: 18px;
    display: flex;
    align-items: center;
    justify-content: center;
    line-height: 1;
    box-shadow: none;
    pointer-events: none;  /* avoids accidental clicks */
}

/* Mobile layout */
@media (max-width: 850px) {
    .nav-indicator {
        position: static;   /* avoid relative + offsets confusion */
        margin-left: 8px;
        box-shadow: none;   /* cleaner inline look */
    }
}

</style>