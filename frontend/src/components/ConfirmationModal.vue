<template>
  <div v-if="visible" class="modal-overlay" @click.self="$emit('close')">
    <div class="modal-content">
      <div class="modal-header" :class="type">
        <h3>{{ title }}</h3>
        <button class="close-btn" @click="$emit('close')">&times;</button>
      </div>
      
      <div class="modal-body">
        <p v-html="message"></p>
      </div>

      <div class="modal-footer">
        <button v-if="showCancel" class="btn-cancel" @click="$emit('close')">
          {{ cancelText }}
        </button>
        <button class="btn-confirm" :class="type" @click="$emit('confirm')">
          {{ confirmText }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
defineProps({
  visible: Boolean,
  title: String,
  message: String,
  type: {
    type: String,
    default: 'info' // info, success, warning, danger
  },
  showCancel: {
    type: Boolean,
    default: true
  },
  confirmText: {
    type: String,
    default: 'Confirm'
  },
  cancelText: {
    type: String,
    default: 'Cancel'
  }
})

defineEmits(['close', 'confirm'])
</script>

<style scoped>
.modal-overlay {
  position: fixed; top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(0, 0, 0, 0.5);
  display: flex; align-items: center; justify-content: center;
  z-index: 9999;
}

.modal-content {
  background: var(--bg-card);
  width: 90%; max-width: 400px;
  border-radius: 0;
  overflow: hidden;
  box-shadow: var(--shadow-lg);
  animation: popIn 0.2s ease-out;
}

@keyframes popIn {
  from { transform: scale(0.95); opacity: 0; }
  to { transform: scale(1); opacity: 1; }
}

.modal-header {
  padding: 20px;
  display: flex; justify-content: space-between; align-items: center;
  background: var(--bg-input);
  border-bottom: 1px solid var(--border-default);
}

.modal-header.danger h3 { color: var(--color-error); }
.modal-header.success h3 { color: var(--color-success); }
.modal-header.info h3 { color: var(--color-info); }
.modal-header.warning h3 { color: var(--color-warning); }

.modal-header h3 {
  margin: 0;
  font-size: 1.1rem;
  font-weight: 700;
}

.close-btn {
  background: none; border: none; font-size: 1.5rem; cursor: pointer; color: var(--text-tertiary);
}
.close-btn:hover { color: var(--color-gray-700); }

.modal-body { padding: 25px 20px; font-size: 0.95rem; color: var(--text-primary); line-height: 1.5; }

.modal-footer {
  padding: 15px 20px;
  background: var(--bg-input);
  border-top: 1px solid var(--border-default);
  display: flex; justify-content: flex-end; gap: 12px;
}

.btn-cancel {
  padding: 8px 16px;
  border: 1px solid var(--border-default);
  background: var(--bg-card);
  color: var(--text-secondary);
  border-radius: 0;
  font-weight: 600;
  cursor: pointer;
}
.btn-cancel:hover { background: var(--border-default); }

.btn-confirm {
  padding: 8px 16px;
  border: none;
  border-radius: 0;
  background: var(--color-primary);
  color: var(--color-white);
  font-weight: 600;
  cursor: pointer;
}

.btn-confirm.danger { background: var(--color-error); color: var(--color-white); }
.btn-confirm.danger:hover { opacity: 0.9; }

.btn-confirm.success { background: var(--color-success); }
.btn-confirm.success:hover { background: #2f855a; }

.btn-confirm.info { background: var(--color-info); }
.btn-confirm.info:hover { background: #2b6cb0; }

.btn-confirm.warning { background: var(--color-warning); }
.btn-confirm.warning:hover { background: #b7791f; }
</style>
