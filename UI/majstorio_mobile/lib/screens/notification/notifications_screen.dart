import 'package:flutter/material.dart';
import 'package:majstorio_mobile/layouts/master_screen.dart';
import 'package:majstorio_mobile/models/notification/notification.dart';
import 'package:majstorio_mobile/models/notification/notification_search_request.dart';
import 'package:majstorio_mobile/providers/notification/notification_provider.dart';
import 'package:majstorio_mobile/theme/app_theme.dart';
import 'package:majstorio_mobile/utils/error_message.dart';
import 'package:majstorio_mobile/utils/formatters.dart';
import 'package:majstorio_mobile/widgets/dialogs.dart';
import 'package:majstorio_mobile/widgets/load_state.dart';
import 'package:majstorio_mobile/widgets/pager.dart';
import 'package:provider/provider.dart';

// "Obavijesti": the user's notifications, newest first. Unread ones are bold with a gold dot.
// Tapping an unread notification marks it as read.
class NotificationsScreen extends StatefulWidget {
  const NotificationsScreen({super.key});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  late NotificationProvider _notificationProvider;

  var notifications = <AppNotification>[];
  var isLoading = true;
  String? error;

  var page = 1;
  var pageSize = 10;
  var totalCount = 0;

  @override
  void initState() {
    _notificationProvider = context.read<NotificationProvider>();
    // A new notification appears while the screen is open.
    _notificationProvider.changes.addListener(loadNotifications);
    loadNotifications();
    super.initState();
  }

  @override
  void dispose() {
    _notificationProvider.changes.removeListener(loadNotifications);
    super.dispose();
  }

  Future<void> loadNotifications() async {
    setState(() {
      isLoading = true;
      error = null;
    });
    try {
      var response = await _notificationProvider.get(NotificationSearchRequest(page: page, pageSize: pageSize));
      if (!mounted) return;
      setState(() {
        notifications = response.items;
        totalCount = response.totalCount;
        isLoading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        error = exceptionText(e);
        isLoading = false;
      });
    }
  }

  Future<void> markAsRead(AppNotification notification) async {
    if (notification.isRead) return;
    try {
      await _notificationProvider.markAsRead(notification.id);
      if (!mounted) return;
      loadNotifications();
    } catch (e) {
      if (!mounted) return;
      showMessage(context, exceptionText(e));
    }
  }

  Future<void> markAllAsRead() async {
    try {
      await _notificationProvider.markAllAsRead();
      if (!mounted) return;
      loadNotifications();
    } catch (e) {
      if (!mounted) return;
      showMessage(context, exceptionText(e));
    }
  }

  Widget buildNotificationCard(AppNotification notification) {
    return Card(
      child: ListTile(
        onTap: () => markAsRead(notification),
        leading: Icon(
          notification.isRead ? Icons.notifications_none : Icons.circle,
          size: notification.isRead ? 24 : 12,
          color: notification.isRead ? AppTheme.textMuted : AppTheme.gold,
        ),
        title: Text(
          notification.title,
          style: TextStyle(
            fontWeight: notification.isRead ? FontWeight.normal : FontWeight.w700,
            color: AppTheme.ink,
          ),
        ),
        subtitle: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(notification.message),
            const SizedBox(height: 4),
            Text(
              formatDateTime(notification.createdAtUtc),
              style: const TextStyle(fontSize: 12, color: AppTheme.textMuted),
            ),
          ],
        ),
      ),
    );
  }

  Widget buildResults() {
    if (isLoading && notifications.isEmpty) return const LoadingView();
    if (error != null) return ErrorView(message: error!, onRetry: loadNotifications);
    if (notifications.isEmpty) {
      return const EmptyView(message: "Nemate obavijesti.", icon: Icons.notifications_none);
    }
    return Column(
      children: [
        for (var notification in notifications)
          Padding(padding: const EdgeInsets.only(bottom: 12), child: buildNotificationCard(notification)),
        Card(
          child: Pager(
            page: page,
            pageSize: pageSize,
            totalCount: totalCount,
            onPageChanged: (newPage) {
              page = newPage;
              loadNotifications();
            },
          ),
        ),
      ],
    );
  }

  @override
  Widget build(BuildContext context) {
    return MasterScreen(
      title: "Obavijesti",
      actions: [
        IconButton(
          icon: const Icon(Icons.done_all),
          tooltip: "Označi sve kao pročitano",
          onPressed: markAllAsRead,
        ),
      ],
      child: RefreshIndicator(
        onRefresh: loadNotifications,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [buildResults()],
        ),
      ),
    );
  }
}
