// What every list request has in common: which page and how many rows per page.
// Mirrors BaseSearchRequest on the backend (the server allows a page size of at most 100).
class BaseSearchRequest {
  final int page;
  final int pageSize;

  BaseSearchRequest({this.page = 1, this.pageSize = 10});

  // The values that go into the URL after the question mark: ?page=1&pageSize=10
  Map<String, String> toQueryParameters() => {
    'page': page.toString(),
    'pageSize': pageSize.toString(),
  };
}
