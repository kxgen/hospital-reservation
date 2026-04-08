<template>
  <main>
    <AlertMessage
      v-if="alert.isVisible"
      :message="alert.message"
      :type="alert.type"
      @close="alert.hideAlert"
    />

    <router-view />
  </main>
</template>

<script setup>
import { onMounted } from 'vue'
import { RouterView } from 'vue-router'
import AlertMessage from '@/components/AlertMessage.vue'
import { useAlertStore } from '@/stores/alertStore'
import { useAuthStore } from '@/stores/auth'

const alert = useAlertStore()
const auth = useAuthStore()

onMounted(() => {
  auth.loadFromStorage()
})
</script>
