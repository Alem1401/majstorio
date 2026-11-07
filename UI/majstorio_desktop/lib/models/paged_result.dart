// One page of any list the API returns: the items of the page and the total across all pages.
class PagedResult<T> {
  final List<T> items;
  final int totalCount;

  PagedResult({required this.items, required this.totalCount});
}
