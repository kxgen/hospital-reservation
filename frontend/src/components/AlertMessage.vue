<template>
  <Transition name="fade">
    <div v-if="visible" :class="['alert-box', type]">
      <div class="icon-section">
        <span v-if="type === 'success'">✓</span>
        <span v-else-if="type === 'error'">✕</span>
        <span v-else-if="type === 'warning'">!</span>
        <span v-else>ℹ</span>
      </div>
      <div class="content">
        <span class="message">{{ message }}</span>
      </div>
      <button class="close-btn" @click="visible = false">&times;</button>
    </div>
  </Transition>
</template>

<script setup>
import { ref, onMounted } from 'vue';

const props = defineProps({
  message: { type: String, required: true },
  type: { 
    type: String, 
    default: 'info',
    validator: (value) => ['success', 'error', 'warning', 'info'].includes(value)
  },
  duration: { type: Number, default: 6000 }
});

const visible = ref(true);

onMounted(() => {
  if (props.duration > 0) {
    setTimeout(() => {
      visible.value = false;
    }, props.duration);
  }
});
</script>

<style scoped>
.alert-box {
  position: fixed;
  top: 30px;
  left: 50%;
  transform: translateX(-50%);
  padding: 1rem 1.5rem;
  border-radius: 12px;
  display: flex;
  align-items: center;
  gap: 15px;
  width: 90%;
  max-width: 500px;
  
  box-shadow: 0 10px 15px -3px rgba(31, 26, 22, 0.1), 0 4px 6px -2px rgba(31, 26, 22, 0.05);
  z-index: 10000;
  border: 2px solid transparent;
}

.icon-section {
  font-weight: 800;
  font-size: 1.2rem;
  display: flex;
  align-items: center;
}

.message {
  font-weight: 700;
  font-size: 0.95rem;
  letter-spacing: -0.01em;
}

.close-btn {
  background: none;
  border: none;
  font-size: 1.4rem;
  cursor: pointer;
  margin-left: auto;
  color: currentColor;
  opacity: 0.4;
  transition: opacity 0.2s;
  padding: 0 5px;
}

.close-btn:hover { opacity: 1; }




.success {
  background-color: var(--color-primary-light);
  color: var(--color-primary);
  border-color: var(--color-primary);
}


.error {
  background-color: #fff1f1;
  color: #a83232; 
  border-color: #a83232;
}


.warning {
  background-color: #fffbeb;
  color: #92400e; 
  border-color: #f59e0b;
}


.info {
  background-color: var(--color-gray-50);
  color: var(--color-gray-700);
  border-color: var(--color-gray-400);
}


.fade-enter-active, .fade-leave-active {
  transition: all 0.4s cubic-bezier(0.4, 0, 0.2, 1);
}

.fade-enter-from, .fade-leave-to {
  opacity: 0;
  transform: translate(-50%, -30px);
}
</style>