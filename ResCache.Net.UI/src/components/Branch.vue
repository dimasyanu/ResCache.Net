<script setup lang="ts">
import type { TreeNode } from '../models/tree-node.model'

defineProps<{
  node: TreeNode
}>()
</script>
<template>
  <details v-if="node.children && node.children.length > 0" open>
    <summary>
      <img src="/folder.svg" class="w-4 h-4 mr-1 invert" />
      {{ node.path }}
    </summary>

    <ul v-if="node.children && node.children.length > 0">
      <li v-for="child in node.children" :key="child.path">
        <Branch :node="child" />
      </li>
    </ul>
  </details>
  <div v-else class="group/entry entry block cursor-default">
    <div class="tooltip tooltip-right" :data-tip="node.path">
      <img
        src="/bolt.svg"
        class="w-4 h-4 mr-1 inline-block align-middle invert"
      />
      <span class="text-xs text-gray-400 inline-block align-middle">{{
        node.path.split('/').pop()
      }}</span>
      <div
        class="delete-btn align-middle ml-2 cursor-pointer group-hover/entry:inline-block hidden"
      >
        <img src="/trash.svg" class="w-4 h-4 invert" />
      </div>
    </div>
  </div>
</template>

<style lang="scss" scoped>
.entry {
  &:active {
    background-color: transparent;
  }
}
</style>
