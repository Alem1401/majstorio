import 'package:json_annotation/json_annotation.dart';

part 'review.g.dart';

// Read only: what is sent is a separate *UpsertRequest.
@JsonSerializable(createToJson: false)
class Review {
  final int id;
  final int appointmentId;
  final String customerName;
  final String tradespersonName;
  final int rating;
  final String? comment;
  final DateTime createdAtUtc;

  Review({
    required this.id,
    required this.appointmentId,
    required this.customerName,
    required this.tradespersonName,
    required this.rating,
    this.comment,
    required this.createdAtUtc,
  });

  factory Review.fromJson(Map<String, dynamic> json) => _$ReviewFromJson(json);
}
