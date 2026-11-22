import 'package:majstorio_mobile/utils/formatters.dart';

// A customer's rating of a completed job. Mirrors ReviewResponse on the backend.
class Review {
  final int id;
  final int appointmentId;
  final String tradespersonName;
  final int rating;
  final String? comment;
  final DateTime createdAtUtc;

  Review({
    required this.id,
    required this.appointmentId,
    required this.tradespersonName,
    required this.rating,
    this.comment,
    required this.createdAtUtc,
  });

  factory Review.fromJson(Map<String, dynamic> json) {
    return Review(
      id: json['id'],
      appointmentId: json['appointmentId'],
      tradespersonName: json['tradespersonName'] ?? '',
      rating: json['rating'],
      comment: json['comment'],
      createdAtUtc: parseUtc(json['createdAtUtc']),
    );
  }
}
