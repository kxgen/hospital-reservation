<script setup>
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '@/stores/auth'
import Logo from '@/components/Logo.vue'
import Skeleton from '@/components/Skeleton.vue'

const props = defineProps({
  isOpen: Boolean,
  portalName: { type: String, default: 'Hospital' },
  links: { type: Array, required: true }, // [{ name: '...', path: '...' }]
  userName: { type: String, default: 'User' },
  userRole: { type: String, default: 'Staff' }
})

const emit = defineEmits(['close'])

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()

function navigate(path) {
  router.push(path)
  emit('close')
}

function logout() {
  auth.logout()
  router.push('/login')
}
</script>

<template>
  <div v-if="isOpen" class="sidebar-overlay" @click="$emit('close')"></div>

  <aside :class="['sidebar-contrast', { 'is-open': isOpen }]">
    <div class="sidebar-header">
      <router-link to="/" class="brand-link">
        <div class="brand-group">
          <div class="brand-logo-svg">
            <Logo class="logo-small"/>
          </div>
          <div class="brand-meta">
            <span class="hospital-name">Trinity<span>Specialized Center</span></span>
          </div>
        </div>
      </router-link>
      <button class="close-trigger" @click="$emit('close')">✕</button>
    </div>

    <!-- User Info moved up -->
    <div class="user-section">
      <div class="profile-summary-header">
        <div class="profile-avatar-small">
          <template v-if="!auth.isLoaded">
            <Skeleton width="100%" height="100%" borderRadius="8px" />
          </template>
          <template v-else>
            {{ userName.charAt(0) }}
          </template>
        </div>
        <div class="profile-info-header">
          <template v-if="!auth.isLoaded">
            <Skeleton width="100px" height="14px" />
          </template>
          <template v-else>
            <p class="profile-name-header">{{ userName }}</p>
            <p class="profile-role-header">{{ userRole }}</p>
          </template>
        </div>
      </div>
    </div>

    <hr class="nav-divider" />

    <div class="sidebar-content">
      <nav class="nav-menu">
        <template v-if="!auth.isLoaded">
          <div v-for="i in 5" :key="i" class="nav-link skeleton-link">
            <Skeleton width="80%" height="16px" />
          </div>
        </template>
        <template v-else>
          <button
            v-for="link in links"
            :key="link.path"
            @click="navigate(link.path)"
            :class="['nav-link', { active: route.path.startsWith(link.path) }]"
          >
            <span class="label">{{ link.name }}</span>
          </button>
        </template>
      </nav>
    </div>

    <div class="sidebar-footer">
      <button class="action-logout" @click="logout" :disabled="!auth.isLoaded">
        Sign Out
      </button>
    </div>
  </aside>
</template>

<style scoped>
.sidebar-contrast {
  position: fixed;
  top: 0;
  left: 0;
  width: 270px;
  height: 100vh;
  background: var(--bg-input); /* Different from bg-body (gray-50) */
  border-right: 1px solid var(--border-default);
  display: flex;
  flex-direction: column;
  z-index: 1001;
  transition: all 0.35s cubic-bezier(0.4, 0, 0.2, 1);
}

.sidebar-header {
  padding: 1.2rem 1.2rem 0.5rem 1.2rem;
}

.brand-link { text-decoration: none; display: block; transition: opacity 0.2s; }
.brand-link:hover { opacity: 0.8; }

.brand-group { display: flex; align-items: center; gap: 10px; }

.brand-logo-svg {
  width: 32px;
  height: 32px;
  background: var(--color-primary);
  color: var(--color-white);
  border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  padding: 6px;
}
.logo-small { width: 100%; height: 100%; }

.brand-meta { display: flex; flex-direction: column; }

.hospital-name { font-size: 1rem; font-weight: 800; color: var(--text-primary); line-height: 1.1; }
.hospital-name span { display: block; margin-top: 3px; color: var(--color-primary); font-weight: 500; font-size: 0.7rem; text-transform: uppercase; letter-spacing: 0.5px; }

.close-trigger { display: none; background: none; border: none; font-size: 1.5rem; color: var(--text-secondary); }

/* User Info Section (Top) */
.user-section { padding: 0 1.2rem 1rem 1.2rem; }
.profile-summary-header {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px;
  border-radius: 12px;
  border: 1px solid var(--border-default);
}
.profile-avatar-small {
  width: 32px; height: 32px;
  background: var(--bg-input);
  color: var(--color-primary);
  border-radius: 8px;
  display: flex; align-items: center; justify-content: center;
  font-weight: 800;
  font-size: 0.9rem;
}
.profile-info-header { display: flex; flex-direction: column; }
.profile-name-header { font-size: 0.85rem; font-weight: 700; color: var(--text-primary); margin: 0; line-height: 1.2; }
.profile-role-header { font-size: 0.7rem; color: var(--text-secondary); font-weight: 600; text-transform: capitalize; margin: 0; }

.nav-divider { border: 0; border-top: 1px solid var(--border-default); margin: 0 1.2rem; }

/* Navigation */
.sidebar-content { flex: 1; padding: 15px 15px; overflow-y: auto; }
.nav-menu { display: flex; flex-direction: column; gap: 5px; }

.nav-link {
  display: flex; align-items: center; padding: 12px 16px; border: none; background: transparent;
  color: var(--text-secondary); border-radius: 10px; cursor: pointer; font-size: 0.9rem; text-align: left; transition: all 0.2s; font-weight: 500;
}
.nav-link:hover { background: rgba(93, 134, 108, 0.08); color: var(--color-primary); }
.nav-link.active { background: var(--color-primary); color: white; box-shadow: var(--shadow-button); font-weight: 700; }

.sidebar-badge { margin-left: auto; background: #ef4444; color: white; font-size: 0.7rem; font-weight: 800; padding: 2px 8px; border-radius: 50px; min-width: 20px; text-align: center; }

/* Footer Section */
.sidebar-footer {
  padding: 20px 25px;
  background: var(--bg-body); 
  border-top: 1px solid var(--border-default);
  margin-top: auto;
}

.action-logout {
  width: 100%;
  padding: 12px;
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  color: var(--text-error);
  border-radius: 10px;
  font-weight: 700;
  font-size: 0.85rem;
  cursor: pointer;
  transition: all 0.2s;
}

.action-logout:hover {
  background: var(--bg-error);
  border-color: var(--bg-error);
  color: var(--text-error);
}

/* Mobile Responsiveness */
@media (max-width: 768px) {
  .sidebar-contrast {
    transform: translateX(-100%);
  }
  .sidebar-contrast.is-open {
    transform: translateX(0);
  }
  .close-trigger {
    display: block;
    position: absolute;
    top: 30px;
    right: 20px;
  }
  .sidebar-overlay {
    position: fixed;
    top: 0; left: 0; right: 0; bottom: 0;
    background: rgba(61, 52, 44, 0.4);
    backdrop-filter: blur(4px);
    z-index: 1000;
  }
}
</style>