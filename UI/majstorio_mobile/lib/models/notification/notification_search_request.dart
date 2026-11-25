import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for GET /Notifications. Mirrors NotificationSearchRequest on the backend.
class NotificationSearchRequest extends BaseSearchRequest {
  final bool? isRead;

  NotificationSearchRequest({super.page, super.pageSize, this.isRead});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (isRead != null) 'isRead': isRead.toString(),
  };
}
