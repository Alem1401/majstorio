import 'package:json_annotation/json_annotation.dart';

part 'review.g.dart';

@JsonSerializable()
class Review {
  int? id;
  final int appointmentId;
  final String customerName;
  final String tradespersonName;
  final int rating;
  final String? comment;
  final DateTime createdAtUtc;

  Review({
    this.id,
    required this.appointmentId,
    required this.customerName,
    required this.tradespersonName,
    required this.rating,
    this.comment,
    required this.createdAtUtc,
  });

  factory Review.fromJson(Map<String, dynamic> json) => _$ReviewFromJson(json);

  Map<String, dynamic> toJson() => _$ReviewToJson(this);
}
