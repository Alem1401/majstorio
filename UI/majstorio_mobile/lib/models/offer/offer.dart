import 'package:majstorio_mobile/utils/formatters.dart';

// A tradesperson's price and time for a request. Mirrors OfferResponse on the backend.
class Offer {
  // The status numbers the API uses for an offer.
  static const int pending = 0;
  static const int accepted = 1;
  static const int withdrawn = 3;
  static const int closed = 4;
  static const Map<int, String> statusNames = {
    0: 'Na čekanju',
    1: 'Prihvaćena',
    3: 'Povučena',
    4: 'Zatvorena',
  };

  final int id;
  final int serviceRequestId;
  final int tradespersonProfileId;
  final String customerName;
  final String tradespersonName;
  final String serviceCategoryName;
  final double price;
  final DateTime proposedStartUtc;
  final int durationMinutes;
  final int status;
  // Why the offer was closed or withdrawn, when the API gives a reason.
  final String? decisionReason;

  Offer({
    required this.id,
    required this.serviceRequestId,
    required this.tradespersonProfileId,
    required this.customerName,
    required this.tradespersonName,
    required this.serviceCategoryName,
    required this.price,
    required this.proposedStartUtc,
    required this.durationMinutes,
    required this.status,
    this.decisionReason,
  });

  // An unknown number gets a neutral label, and no action is offered for it.
  String get statusName => statusNames[status] ?? 'Nepoznato';
  bool get isPending => status == pending;

  factory Offer.fromJson(Map<String, dynamic> json) {
    return Offer(
      id: json['id'],
      serviceRequestId: json['serviceRequestId'],
      tradespersonProfileId: json['tradespersonProfileId'],
      customerName: json['customerName'] ?? '',
      tradespersonName: json['tradespersonName'] ?? '',
      serviceCategoryName: json['serviceCategoryName'] ?? '',
      price: (json['price'] as num).toDouble(),
      proposedStartUtc: parseUtc(json['proposedStartUtc']),
      durationMinutes: json['durationMinutes'],
      status: json['status'],
      decisionReason: json['decisionReason'],
    );
  }
}
