<template>
  <Transition name="modal">
    <div v-if="isOpen" class="modal-overlay" @click.self="$emit('close')">
      <div class="modal-content">
        <div class="modal-header">
          <div class="header-icon">
            <span class="icon">🔒</span>
          </div>
          <h2>Security Reset</h2>
          <p>Issue a new temporary password for <strong>{{ userName }}</strong>.</p>
        </div>

        <div class="modal-body">
          <div class="form-group">
            <label for="temp-password">Temporary Password</label>
            <div class="input-wrapper">
              <input 
                id="temp-password"
                :type="showPassword ? 'text' : 'password'" 
                v-model="password" 
                placeholder="Enter new password..."
                class="styled-input"
                ref="passwordInput"
              />
              <button class="toggle-visibility" @click="showPassword = !showPassword">
                {{ showPassword ? 'Hide' : 'Show' }}
              </button>
            </div>
          </div>
          <div class="info-alert">
            <span class="info-icon">ℹ️</span>
            <p>The user will be required to change this password on their next login.</p>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-cancel" @click="$emit('close')">Cancel</button>
          <button 
            class="btn-confirm" 
            :disabled="!password || isSaving" 
            @click="handleConfirm"
          >
            {{ isSaving ? 'Resetting...' : 'Confirm Reset' }}
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { ref, watch } from 'vue';

const props = defineProps({
  isOpen: Boolean,
  userName: String,
  isSaving: Boolean
});

const emit = defineEmits(['close', 'confirm']);

const password = ref('');
const showPassword = ref(false);
const passwordInput = ref(null);

watch(() => props.isOpen, (newVal) => {
  if (newVal) {
    password.value = '';
    showPassword.value = false;
  }
});

const handleConfirm = () => {
  if (password.value) {
    emit('confirm', password.value);
  }
};
</script>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0;
  left: 0;
  width: 100%;
  height: 100%;
  background: rgba(15, 23, 42, 0.7);
  backdrop-filter: blur(8px);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 9999;
}

.modal-content {
  background: var(--bg-card);
  width: 100%;
  max-width: 450px;
  border-radius: 0;
  padding: 40px;
  box-shadow: var(--shadow-lg);
  border: 1px solid var(--border-default);
}

.modal-header {
  text-align: center;
  margin-bottom: 30px;
}

.header-icon {
  width: 64px;
  height: 64px;
  background: var(--color-primary-light);
  border-radius: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 20px;
}

.icon {
  font-size: 32px;
}

.modal-header h2 {
  margin: 0 0 10px;
  font-size: 1.5rem;
  color: var(--text-primary);
  font-weight: 800;
}

.modal-header p {
  color: var(--text-secondary);
  font-size: 0.95rem;
  margin: 0;
}

.modal-body {
  margin-bottom: 30px;
}

.form-group label {
  display: block;
  font-size: 0.85rem;
  font-weight: 700;
  color: var(--text-tertiary);
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.05em;
}

.input-wrapper {
  position: relative;
  display: flex;
  align-items: center;
}

.styled-input {
  width: 100%;
  padding: 14px 16px;
  padding-right: 60px;
  border: 1px solid var(--border-input);
  border-radius: 0;
  font-size: 1rem;
  transition: all 0.2s ease;
  box-sizing: border-box;
}

.styled-input:focus {
  outline: none;
  border-color: var(--color-primary);
  box-shadow: 0 0 0 4px var(--color-primary-glow);
}

.toggle-visibility {
  position: absolute;
  right: 12px;
  background: none;
  border: none;
  color: var(--color-primary);
  font-weight: 700;
  font-size: 0.75rem;
  cursor: pointer;
  text-transform: uppercase;
}

.info-alert {
  display: flex;
  gap: 12px;
  background: var(--bg-body);
  padding: 12px 16px;
  border-radius: 0;
  margin-top: 20px;
  border: 1px solid var(--border-default);
}

.info-icon {
  font-size: 1.1rem;
}

.info-alert p {
  margin: 0;
  font-size: 0.85rem;
  color: var(--text-secondary);
  line-height: 1.4;
}

.modal-footer {
  display: grid;
  grid-template-columns: 1fr 1fr;
  gap: 15px;
}

.btn-cancel {
  padding: 14px;
  border-radius: 0;
  border: 1.5px solid var(--border-default);
  background: white;
  color: var(--text-primary);
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-cancel:hover {
  background: var(--bg-input);
}

.btn-confirm {
  padding: 14px;
  border-radius: 0;
  border: none;
  background: var(--color-primary);
  color: white;
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
}

.btn-confirm:hover:not(:disabled) {
  opacity: 0.9;
  transform: translateY(-1px);
}

.btn-confirm:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

/* Transitions */
.modal-enter-active,
.modal-leave-active {
  transition: opacity 0.3s ease;
}

.modal-enter-from,
.modal-leave-to {
  opacity: 0;
}

.modal-enter-active .modal-content {
  animation: modal-in 0.3s cubic-bezier(0.34, 1.56, 0.64, 1);
}

@keyframes modal-in {
  from {
    transform: scale(0.9) translateY(20px);
    opacity: 0;
  }
  to {
    transform: scale(1) translateY(0);
    opacity: 1;
  }
}
</style>
