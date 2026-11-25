import 'package:majstorio_mobile/utils/formatters.dart';

// A message for the signed-in user, for example "Nova ponuda". Mirrors NotificationResponse on the backend.
class AppNotification {
  final int id;
  final int? serviceRequestId;
  final String title;
  final String message;
  final bool isRead;
  final DateTime createdAtUtc;

  AppNotification({
    required this.id,
    this.serviceRequestId,
    required this.title,
    required this.message,
    required this.isRead,
    required this.createdAtUtc,
  });

  factory AppNotification.fromJson(Map<String, dynamic> json) {
    return AppNotification(
      id: json['id'],
      serviceRequestId: json['serviceRequestId'],
      title: json['title'] ?? '',
      message: json['message'] ?? '',
      isRead: json['isRead'] ?? false,
      createdAtUtc: parseUtc(json['createdAtUtc']),
    );
  }
}
