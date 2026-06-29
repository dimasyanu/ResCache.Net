<script setup lang="ts">
import { onMounted, ref } from 'vue'
import Branch from './Branch.vue'
import type { CacheEntry } from '../models/cache-entry.model'
import type { TreeNode } from '../models/tree-node.model'

const baseUrl = import.meta.env.VITE_API_BASE_URL

const treeData = ref<TreeNode[]>([])

const makeTreeNode = (path: string): TreeNode => {
  return { path, children: [] }
}

const loadTree = async () => {
  // const url = `${baseUrl}/Api/Cache/Entries`
  const url = '/dummies.json'
  const list: CacheEntry[] = await fetch(url).then((res) => res.json())

  const rootNode: TreeNode = { path: '', children: [] }
  for (const item of list) {
    const segments = item.path
      .split('/')
      .filter((segment) => segment.trim().length > 0)

    let currentNode: TreeNode = rootNode
    let currentPath = ''
    for (let i = 0; i < segments.length; i++) {
      const segment = segments[i]
      currentPath += `/${segment}`

      const childNode = currentNode.children?.find(
        (node) => node.path === currentPath,
      )
      if (!!childNode) {
        currentNode = childNode
        continue
      }

      currentNode.children?.push({ path: currentPath, children: [] })
      currentNode = currentNode.children?.find(
        (node) => node.path === currentPath,
      )!
    }
  }

  return rootNode.children || []
}

onMounted(async () => {
  const results = await loadTree()
  treeData.value = results
  console.log(results)
})
</script>
<template>
  <div class="tree-container">
    <ul class="menu menu-xs bg-base-200 rounded-box w-full overflow-y-auto">
      <li v-for="node in treeData" :key="node.path">
        <Branch :node="node" />
      </li>
    </ul>
  </div>
</template>

<style lang="scss" scoped>
.tree-container {
  display: flex;
  justify-content: center;
  align-items: center;
  height: 100%;
  padding-block: 2rem;
  max-width: 512px;
  margin-inline: auto;
}

@media (max-width: 768px) {
  .tree-container {
    padding: 2rem;
  }
}
</style>
