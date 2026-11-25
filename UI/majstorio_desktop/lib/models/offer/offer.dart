import 'package:json_annotation/json_annotation.dart';

part 'offer.g.dart';

// Read only: what is sent is a separate *UpsertRequest.
@JsonSerializable(createToJson: false)
class Offer {
  // The status numbers the API uses for an offer.
  static const int pending = 0;
  static const Map<int, String> statusNames = {
    0: 'Pending',
    1: 'Accepted',
    2: 'Rejected',
    3: 'Withdrawn',
    4: 'Closed',
  };

  final int id;
  final String customerName;
  final String tradespersonName;
  final String serviceCategoryName;
  final double price;
  final DateTime proposedStartUtc;
  final int durationMinutes;
  final int status;

  Offer({
    required this.id,
    required this.customerName,
    required this.tradespersonName,
    required this.serviceCategoryName,
    required this.price,
    required this.proposedStartUtc,
    required this.durationMinutes,
    required this.status,
  });

  String get statusName => statusNames[status] ?? 'Unknown';

  factory Offer.fromJson(Map<String, dynamic> json) => _$OfferFromJson(json);
}
