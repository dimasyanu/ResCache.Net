import type { CacheEntry } from './cache-entry.model'

export interface TreeNode {
  path: string
  children?: TreeNode[]
  data?: CacheEntry
}
