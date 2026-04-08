<template>
  <div class="admin-container">
    <div class="page-header">
      <div class="header-content">
        <h1>System <span>Logs</span></h1>
        <p>Monitor administrative actions and system changes across the portal.</p>
      </div>
      <div class="header-actions">
        <button class="btn-action-outline" @click="resetFilters">Reset Filters</button>
      </div>
    </div>

    <div class="filter-card shadow-sm">
      <div class="filter-group">
        <label>Filter by Date</label>
        <input 
          type="date" 
          v-model="filter.date" 
          class="styled-search-select" 
          @change="loadLogs(1)"
        />
      </div>

      <div class="filter-stats">
        <span class="results-count">Total <strong>{{ totalCount }}</strong> activity logs</span>
      </div>
    </div>

    <div class="content-card">
      <div class="table-responsive">
        <table class="premium-table">
          <thead>
            <tr>
              <th width="40">#</th>
              <th width="160">Date</th>
              <th width="40">Time</th>
              <th class="text-left">Action</th>
              <th class="text-right">Actor</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(log, index) in logs" :key="log.id" class="log-row">
              <td class="index-cell">{{ ((currentPage - 1) * pageSize) + index + 1 }}</td>
              <td class="timestamp-cell">
                <div class="date-badge">{{ $dt.date(log.timestamp) }}</div>
              </td>
              <td>
                <span class="time-subtext">{{ $dt.time(log.timestamp) }}</span>
              </td>
              <td class="action-cell text-left">
                <span class="action-main"><strong>{{ log.action }}</strong></span>
                <span class="detail-text">{{ log.details }}</span>
              </td>

              <td class="text-right">
                <span class="user-badge" :title="log.actorName">
                  {{ log.actorName }}
                </span>
              </td>
            </tr>

            <tr v-if="logs.length === 0">
              <td colspan="4" class="empty-state">
                <p>No activity logs match your current filters.</p>
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>

    <Pagination 
      :current-page="currentPage" 
      :total-pages="totalPages" 
      @change="handlePageChange"
    />
  </div>
</template>

<script setup>
import { ref, onMounted, computed, watch } from "vue";
import axios from "@/api/axios";
import Pagination from "@/components/Pagination.vue";

const logs = ref([]);
const totalPages = ref(1);
const totalCount = ref(0);
const currentPage = ref(1);
const pageSize = ref(10);
const isLoading = ref(false);

const filter = ref({
  date: ""
});


const loadLogs = async (page = 1) => {
  isLoading.value = true;
  currentPage.value = page;
  try {
    const token = localStorage.getItem('token');
    const params = { 
        page, 
        pageSize: pageSize.value,
        date: filter.value.date || null
    };
    
    const res = await axios.get("/api/admin/logs", {
        params,
        headers: { Authorization: `Bearer ${token}` }
    });
    
    logs.value = res.data.items || [];
    totalPages.value = res.data.totalPages || 1;
    totalCount.value = res.data.totalCount || 0;
  } catch (err) {
    console.error("Failed to load logs:", err);
  } finally {
    isLoading.value = false;
  }
};

const handlePageChange = (newPage) => {
  loadLogs(newPage);
};

const resetFilters = () => {
  filter.value.date = "";
  loadLogs(1);
};

onMounted(() => loadLogs(1));
</script>

<style scoped>
.doctor-container {
  max-width: 1100px;
  margin: 40px auto;
  padding: 0 20px;
}

/* Header & Search Area Alignment */
.page-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 20px;
}

.header-content h1 {
  font-weight: 800;
  font-size: 2.2rem; /* Sync with global */
  color: var(--text-primary);
  margin: 0;
}

.header-content h1 span { color: var(--color-primary); }
.header-content p { color: var(--text-secondary); margin-top: 8px; font-size: 1.1rem; }

/* Filter Card Sync */
.filter-card {
  background: var(--bg-card);
  padding: 20px 25px;
  border-radius: 0;
  border: 1px solid var(--border-default);
  display: flex;
  align-items: flex-end;
  gap: 20px;
  margin-bottom: 25px;
  box-shadow: var(--shadow-sm);
}

.filter-group {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.filter-group label {
  font-size: 0.75rem;
  font-weight: 800;
  color: var(--text-tertiary);
  text-transform: uppercase;
}

.styled-search-select {
  padding: 10px 15px;
  border: 1px solid var(--border-input);
  border-radius: 0;
  min-width: 200px;
  font-size: 0.9rem;
  background: var(--bg-card);
  color: var(--text-primary);
}

.filter-stats {
  margin-left: auto;
  font-size: 0.85rem;
  color: var(--text-secondary);
  padding-bottom: 10px;
}

/* Cell Specific Styling */
.date-badge {
  background: var(--color-primary-light);
  color: var(--color-primary);
  padding: 4px 10px;
  border-radius: 0;
  display: inline-block;
  font-weight: 700;
  font-size: 0.85rem;
  border: 1px solid var(--color-primary-border);
}

.time-subtext {
  display: block;
  font-size: 0.75rem;
  color: var(--text-tertiary);
  margin-top: 4px;
}

.action-main {
  display: block;
  color: var(--text-primary);
}

.detail-text {
  display: block;
  font-size: 0.8rem;
  color: var(--text-secondary);
  margin-top: 4px;
}

/* User Badges Sync */
.user-badge {
  padding: 5px 12px;
  border-radius: 0;
  font-size: 0.75rem;
  font-weight: bold;
  text-transform: uppercase;
  background: var(--bg-input);
  color: var(--text-secondary);
}

.user-badge.admin, .user-badge.doctor {
  background: var(--color-primary-light);
  color: var(--color-primary);
}

.user-badge.receptionist {
  background: var(--color-gray-100);
  color: var(--color-info);
}

@media (max-width: 800px) {
  .filter-card { flex-direction: column; align-items: stretch; }
  .filter-stats { margin-left: 0; text-align: center; }
}
</style>