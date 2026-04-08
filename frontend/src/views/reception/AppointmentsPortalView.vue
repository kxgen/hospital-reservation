<script setup>
import { ref, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import BookAppointmentView from './BookAppointmentView.vue'
import ManageAppointmentsView from './ManageAppointmentsView.vue'

const route = useRoute()
const router = useRouter()

// Determine active tab based on query param or default
// Default to 'manage' or query param
const activeTab = ref(route.query.tab || 'manage')

watch(() => route.query.tab, (newTab) => {
  if (newTab) activeTab.value = newTab
})

const setTab = (tab) => {
  activeTab.value = tab
  router.replace({ query: { ...route.query, tab } })
}
</script>

<template>
  <div class="appointments-portal">
    <div class="portal-nav shadow-sm">
      <button 
        :class="['tab-btn', { active: activeTab === 'manage' }]" 
        @click="setTab('manage')"
      >
        Manage Bookings
      </button>
      <button 
        :class="['tab-btn', { active: activeTab === 'book' }]" 
        @click="setTab('book')"
      >
        Book Appointment
      </button>
    </div>

    <div class="portal-content">
      <transition name="fade" mode="out-in">
        <div v-if="activeTab === 'book'" key="book">
          <BookAppointmentView :is-embedded="true" />
        </div>
        <div v-else-if="activeTab === 'manage'" key="manage">
          <ManageAppointmentsView />
        </div>
      </transition>
    </div>
  </div>
</template>

<style scoped>
.appointments-portal {
  animation: fadeIn 0.4s ease-out;
}

@keyframes fadeIn {
  from { opacity: 0; transform: translateY(10px); }
  to { opacity: 1; transform: translateY(0); }
}

.portal-nav {
  display: flex;
  background: var(--bg-card);
  border: 1px solid var(--border-default);
  border-radius: 0;
  padding: 8px;
  margin-bottom: 2rem;
  gap: 8px;
  max-width: 500px;
}

.tab-btn {
  flex: 1;
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  padding: 12px 20px;
  border: none;
  background: transparent;
  color: var(--text-secondary);
  font-weight: 700;
  font-size: 0.95rem;
  border-radius: 0;
  cursor: pointer;
  transition: all 0.2s;
}

.tab-btn:hover {
  background: var(--bg-input);
  color: var(--text-primary);
}

.tab-btn.active {
  background: var(--color-primary);
  color: white;
  box-shadow: none;
}

.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
  transform: translateY(5px);
}

.portal-content {
  min-height: 400px;
}
</style>
