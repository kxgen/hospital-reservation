<template>
  <div class="admin-container">
    <div class="page-header">
      <div class="header-content">
        <h1>Account <span>Management</span></h1>
        <p>Manage all system users including doctors, administrative staff, and patients.</p>
      </div>
      <div class="header-actions">
        <button class="btn-primary" @click="router.push('/admin/staff/add')">
          <span class="btn-icon">+</span> Register Staff
        </button>
      </div>
    </div>

    <div class="tab-scroller">
      <div class="tabs-modern">
        <button 
          v-for="tab in tabs" 
          :key="tab.id"
          :class="['tab-pill', { active: currentTab === tab.id }]" 
          @click="currentTab = tab.id"
        >
          <span class="tab-label">{{ tab.label }}</span>
          <span v-if="currentTab === tab.id" class="tab-count">{{ filteredUsers.length }}</span>
        </button>
      </div>
    </div>

    <div class="content-card">
      <div class="table-responsive">
        <table class="management-table">
          <thead>
            <tr>
              <th style="width: 60px;">#</th>
              <th class="text-left" style="width: auto;">Full Name & Identity</th>
              <th style="width: 180px;">Account Status</th>
              <th style="width: 160px;">Management</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(user, index) in filteredUsers" :key="user.id" class="user-row">
              <td class="index-cell">
                {{ index + 1 }}
              </td>
              <td class="name-cell text-left">
                <span class="full-name">{{ user.firstName }} {{ user.lastName }}</span>
              </td>

              <td>
                <span :class="['status-pill', (user.isSuspended || user.isDisabled) ? 'status-error' : 'status-active']">
                  {{ (user.isSuspended || user.isDisabled) ? "Suspended" : "Active" }}
                </span>
              </td>

              <td class="actions">
                <button 
                  class="btn-manage" 
                  @click="goToManage(user)"
                >
                  Manage
                </button>
              </td>
            </tr>

            <tr v-if="filteredUsers.length === 0 && !isLoading">
              <td colspan="4" class="empty-state">
                <div class="empty-content">
                  <span class="empty-icon">🔍</span>
                  <p>No {{ currentTab }}s found matching your search.</p>
                </div>
              </td>
            </tr>
            <tr v-if="isLoading">
               <td colspan="4" class="empty-state">
                <LoadingBar />
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, watch } from "vue";
import { useRouter } from "vue-router";
import axios from "@/api/axios";
import LoadingBar from "@/components/LoadingBar.vue";
import { useAlertStore } from "@/stores/alertStore";

const router = useRouter();
const alertStore = useAlertStore();

const currentTab = ref("patient");
const searchQuery = ref("");
const isLoading = ref(false);
const users = ref([]);

const tabs = [
  { id: "patient", label: "Patients" },
  { id: "doctor", label: "Doctors" },
  { id: "receptionist", label: "Receptionists" }
];

const loadData = async () => {
  isLoading.value = true;
  users.value = [];
  try {
    const token = localStorage.getItem('token');
    let endpoint = "";
    
    if (currentTab.value === 'patient') {
      endpoint = "/api/admin/patients";
    } else {
      endpoint = "/api/admin/staff";
    }

    const res = await axios.get(endpoint, {
      headers: { Authorization: `Bearer ${token}` }
    });
    
    let rawData = Array.isArray(res.data) ? res.data : [];
    
    if (currentTab.value === 'patient') {
      // Only list patients who HAVE accounts
      users.value = rawData
        .filter(p => p.accountId !== null && p.accountId !== undefined)
        .map(p => ({
          id: p.patientId,
          firstName: p.firstName,
          lastName: p.lastName,
          email: p.email || "N/A",
          phone: p.phone,
          isDisabled: p.isDisabled,
          role: 'patient'
        }));
    } else {
      // For staff, filter by role
      users.value = rawData.filter(s => s.role?.toLowerCase() === currentTab.value).map(s => ({
        ...s,
        id: s.id, // Ensure we use 'id' (AccountId) for staff
        isSuspended: s.isSuspended
      }));
    }
  } catch (err) {
    console.error("Failed to load data:", err);
    alertStore.showAlert("Failed to load users list", "error");
  } finally {
    isLoading.value = false;
  }
};

const filteredUsers = computed(() => {
  const query = searchQuery.value.toLowerCase();
  return users.value.filter(u => {
    const fullName = `${u.firstName} ${u.lastName}`.toLowerCase();
    return fullName.includes(query) || u.email?.toLowerCase().includes(query);
  });
});

const goToManage = (user) => {
  router.push({
    name: 'admin-account-manage',
    params: { id: user.id },
    query: { role: user.role }
  });
};

watch(currentTab, loadData);
onMounted(loadData);
</script>

<style scoped>
.header-actions {
  display: flex;
  align-items: center;
  gap: 15px;
}

.btn-icon {
  font-size: 1.2rem;
  font-weight: 400;
}

.tab-scroller {
  margin-bottom: 30px;
  padding: 4px;
  background: var(--bg-input);
  border-radius: 0; /* Sharp edges */
  display: inline-flex;
}

.tabs-modern {
  display: flex;
  gap: 4px;
}

.tab-pill {
  background: transparent;
  border: none;
  padding: 10px 24px;
  border-radius: 0; /* Sharp edges */
  font-weight: 600;
  color: var(--text-secondary);
  cursor: pointer;
  transition: all 0.2s ease;
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 0.95rem;
}

.tab-pill.active {
  background: var(--bg-card);
  color: var(--color-primary);
  box-shadow: var(--shadow-sm);
}

.tab-count {
  background: var(--color-primary-light);
  color: var(--color-primary);
  padding: 2px 8px;
  border-radius: 20px;
  font-size: 0.75rem;
  font-weight: 700;
}

.index-cell {
  color: var(--text-tertiary);
  width: 60px;
}

.btn-manage {
  background: var(--bg-card);
  border: 1.5px solid var(--color-primary);
  color: var(--color-primary);
  padding: 8px 18px;
  border-radius: 0; /* Sharp edges */
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-manage:hover {
  background: var(--color-primary);
  color: white;
}

@media (max-width: 900px) {
  .page-header { flex-direction: column; align-items: stretch; }
  .header-actions { flex-direction: column; align-items: stretch; gap: 10px; }
}
</style>
