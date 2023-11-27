export interface Pagination<T> {
  hasNextPage: boolean;
  hasPreviousPage: boolean;
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}
