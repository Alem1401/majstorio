import 'package:majstorio_mobile/utils/formatters.dart';

// What the tradesperson sends when creating (POST /Offers) or editing (PUT /Offers/{id}) an offer.
// The request id is only sent on create; an offer cannot move to another request.
class OfferUpsertRequest {
  final int? serviceRequestId;
  final double price;
  final DateTime proposedStart;
  final int durationMinutes;

  OfferUpsertRequest({
    this.serviceRequestId,
    required this.price,
    required this.proposedStart,
    required this.durationMinutes,
  });

  Map<String, dynamic> toJson() => {
    if (serviceRequestId != null) 'serviceRequestId': serviceRequestId,
    'price': price,
    'proposedStartUtc': toApiDate(proposedStart),
    'durationMinutes': durationMinutes,
  };
}
