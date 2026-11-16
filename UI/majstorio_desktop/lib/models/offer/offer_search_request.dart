import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the offer list. Mirrors OfferSearchRequest on the backend.
class OfferSearchRequest extends BaseSearchRequest {
  // One of the numbers in Offer.statusNames; null shows every status.
  final int? status;

  OfferSearchRequest({super.page, super.pageSize, this.status});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (status != null) 'status': status.toString(),
  };
}
