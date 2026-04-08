<template>
  <div class="pagination-container" v-if="totalPages > 1">
    <button 
      class="page-btn" 
      :disabled="currentPage === 1" 
      @click="$emit('change', 1)"
    >
      First
    </button>

    <button 
      class="page-btn" 
      :disabled="currentPage === 1" 
      @click="$emit('change', currentPage - 1)"
    >
      &laquo; Pref
    </button>

    <div class="page-numbers">
      <button 
        v-for="page in visiblePages" 
        :key="page"
        :class="['page-number', { active: page === currentPage }]"
        @click="$emit('change', page)"
      >
        {{ page }}
      </button>
    </div>

    <button 
      class="page-btn" 
      :disabled="currentPage === totalPages" 
      @click="$emit('change', currentPage + 1)"
    >
      Next &raquo;
    </button>

    <button 
      class="page-btn" 
      :disabled="currentPage === totalPages" 
      @click="$emit('change', totalPages)"
    >
      Last
    </button>
  </div>
</template>

<script setup>
import { computed } from 'vue';

const props = defineProps({
  currentPage: Number,
  totalPages: Number,
  maxVisible: { type: Number, default: 5 }
});

const emit = defineEmits(['change']);

const visiblePages = computed(() => {
  const pages = [];
  let start = Math.max(1, props.currentPage - Math.floor(props.maxVisible / 2));
  let end = Math.min(props.totalPages, start + props.maxVisible - 1);

  if (end - start + 1 < props.maxVisible) {
    start = Math.max(1, end - props.maxVisible + 1);
  }

  for (let i = start; i <= end; i++) {
    pages.push(i);
  }
  return pages;
});
</script>

<style scoped>
.pagination-container {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 15px;
  margin-top: 30px;
  padding: 20px 0;
}

.page-btn {
  padding: 8px 16px;
  border-radius: 0;
  border: 1px solid var(--border-default);
  background: var(--bg-card);
  color: var(--text-primary);
  font-weight: 700;
  cursor: pointer;
  transition: all 0.2s;
  font-size: 0.9rem;
}

.page-btn:hover:not(:disabled) {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.page-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.page-numbers {
  display: flex;
  gap: 8px;
}

.page-number {
  width: 38px;
  height: 38px;
  display: flex;
  align-items: center;
  justify-content: center;
  border-radius: 0;
  border: 1px solid var(--border-default);
  background: var(--bg-card);
  color: var(--text-secondary);
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s;
}

.page-number:hover {
  border-color: var(--color-primary);
  color: var(--color-primary);
}

.page-number.active {
  background: var(--color-primary);
  color: white;
  border-color: var(--color-primary);
  box-shadow: 0 4px 10px var(--color-primary-glow);
}
</style>
