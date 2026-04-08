<script setup>
import { useRouter } from 'vue-router'
import { formatTime, formatDate } from '@/utils/dateFormatter'

const router = useRouter()

const props = defineProps({
  isOpen: Boolean,
  dbId: [String, Number],
  patientName: String,
  patientId: String,
  appointmentTime: String,
  category: String,
  status: String,
  appointmentType: String
});

const emit = defineEmits(['close', 'update-status']);

const goToOutcome = () => {
  emit('close');
  router.push({ 
    name: 'appointment-session', 
    params: { id: props.dbId } 
  });
};

const handleConfirm = () => {
  emit('update-status', 'Confirmed');
};
</script>

<template>
  <Transition name="fade">
    <div v-if="isOpen" class="modal-overlay" @click.self="$emit('close')">
      <div class="modal-window shadow-xl">
        
        <div class="modal-banner" :class="status.toLowerCase()">
          <div class="banner-content">
             <span class="status-label">{{ status }}</span>
             <button class="close-x" @click="$emit('close')">&times;</button>
          </div>
        </div>

        <div class="modal-body">
          <header class="patient-header">
            <div class="avatar-large">{{ patientName?.charAt(0) || 'P' }}</div>
            <div class="header-meta">
              <h2>{{ patientName }}</h2>
              <div class="type-badge" :class="appointmentType?.toLowerCase()">
                {{ appointmentType || 'New Visit' }}
              </div>
            </div>
          </header>

          <div class="info-grid">
            <div class="info-item">
              <label>Time & Date</label>
              <div class="value-with-icon">
                <i class="icon">🕒</i>
                <span>{{ formatTime(appointmentTime) }} — {{ formatDate(appointmentTime) }}</span>
              </div>
            </div>
            
            <div class="info-item">
              <label>Clinical Reason</label>
              <div class="value-with-icon">
                <i class="icon">🩺</i>
                <span>{{ category || 'General Consultation' }}</span>
              </div>
            </div>
          </div>

          <div class="action-zone" v-if="status.toLowerCase() === 'scheduled'">
             <div class="zone-header">Quick Actions</div>
             <button 
               class="btn-action-primary confirm" 
               @click="handleConfirm"
             >
               Confirm Appointment
             </button>
          </div>
        </div>

        <div class="modal-footer">
          <button class="btn-dismiss" @click="$emit('close')">Close</button>
          <button class="btn-start" @click="goToOutcome">
            Start Session
          </button>
        </div>
      </div>
    </div>
  </Transition>
</template>

<style scoped>
.modal-overlay {
  position: fixed;
  top: 0; left: 0; right: 0; bottom: 0;
  background: rgba(15, 23, 42, 0.4);
  backdrop-filter: blur(8px);
  display: flex;
  justify-content: center;
  align-items: center;
  z-index: 2000;
  padding: 20px;
}

.modal-window {
  background: var(--bg-surface);
  width: 100%;
  max-width: 480px;
  border-radius: 24px;
  overflow: hidden;
  box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
  max-height: 80vh;
  display: flex;
  flex-direction: column;
}

.modal-banner {
  padding: 10px 24px;
  background: var(--color-gray-100);
}
.modal-banner.scheduled, .modal-banner.confirmed { background: var(--color-primary-light); color: var(--color-primary); }
.modal-banner.completed { background: #dcfce7; color: #166534; }
.modal-banner.cancelled { background: #fee2e2; color: #991b1b; }

.banner-content { display: flex; justify-content: space-between; align-items: center; }
.status-label { font-size: 0.65rem; font-weight: 800; text-transform: uppercase; letter-spacing: 1px; }
.close-x { background: none; border: none; font-size: 1.5rem; cursor: pointer; color: inherit; opacity: 0.6; }

.modal-body { padding: 32px; overflow-y: auto; flex: 1; }

.patient-header { display: flex; align-items: center; gap: 20px; margin-bottom: 25px; }
.avatar-large {
  width: 56px; height: 56px; border-radius: 16px;
  background: var(--color-primary); color: white;
  display: flex; align-items: center; justify-content: center;
  font-size: 1.5rem; font-weight: 800;
}
.header-meta h2 { margin: 0; font-size: 1.3rem; color: var(--text-main); }

.type-badge {
  display: inline-block; padding: 4px 10px; border-radius: 6px;
  font-size: 0.7rem; font-weight: 800; margin-top: 4px;
  background: var(--color-gray-100); color: var(--color-gray-600);
  text-transform: uppercase;
}
.type-badge.follow-up { background: #fef3c7; color: #92400e; }
.type-badge.new { background: #e0f2fe; color: #0369a1; }

.info-grid { display: grid; grid-template-columns: 1fr; gap: 20px; margin-bottom: 30px; }
.info-item label { 
  display: block; font-size: 0.65rem; font-weight: 800; 
  color: var(--text-light); text-transform: uppercase; margin-bottom: 6px; 
}
.value-with-icon { display: flex; align-items: center; gap: 10px; }
.value-with-icon .icon { font-style: normal; font-size: 1.1rem; }
.value-with-icon span { font-size: 1rem; font-weight: 700; color: var(--text-main); }

.action-zone {
  background: var(--bg-body); border-radius: 16px; padding: 16px;
  display: flex; flex-direction: column; gap: 10px;
}
.zone-header { font-size: 0.65rem; font-weight: 800; color: var(--text-light); text-transform: uppercase; margin-bottom: 2px; }

.btn-action-primary {
  width: 100%; padding: 12px; border: none; border-radius: 12px;
  font-weight: 700; cursor: pointer; transition: 0.2s;
  background: var(--color-primary); color: white;
}
.btn-action-primary:hover { filter: brightness(1.1); transform: translateY(-1px); }

.modal-footer {
  padding: 20px 32px; background: var(--bg-muted);
  display: flex; justify-content: space-between; gap: 16px;
}

.btn-dismiss { background: none; border: none; font-weight: 700; color: var(--text-muted); cursor: pointer; }
.btn-start {
  background: var(--text-main); color: white; border: none;
  padding: 10px 20px; border-radius: 10px; font-weight: 800;
  cursor: pointer; transition: 0.2s;
}
.btn-start:hover { transform: translateY(-2px); box-shadow: 0 4px 12px rgba(0,0,0,0.1); }

.fade-enter-active, .fade-leave-active { transition: opacity 0.3s; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>