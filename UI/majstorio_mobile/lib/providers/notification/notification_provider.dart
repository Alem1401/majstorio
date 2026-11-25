import 'package:flutter/foundation.dart';
import 'package:majstorio_mobile/models/notification/notification.dart';
import 'package:majstorio_mobile/models/notification/notification_search_request.dart';
import 'package:majstorio_mobile/providers/base_provider.dart';

// The signed-in user's notifications. The API returns only the caller's own rows.
class NotificationProvider extends BaseProvider<AppNotification> {
  NotificationProvider() : super("Notifications", AppNotification.fromJson);

  // Goes up when the server reports news; the badge and the list listen and load again.
  final changes = ValueNotifier<int>(0);

  // How many notifications are unread: one row is enough, the total comes with it.
  Future<int> getUnreadCount() async {
    var page = await get(NotificationSearchRequest(isRead: false, pageSize: 1));
    return page.totalCount;
  }

  Future<void> markAsRead(int id) async {
    await command("PUT", "Notifications/$id/read");
  }

  Future<void> markAllAsRead() async {
    await command("PUT", "Notifications/read-all");
  }
}
