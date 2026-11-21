import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for GET /Offers/mine. Mirrors OfferSearchRequest on the backend.
class OfferSearchRequest extends BaseSearchRequest {
  final int? status;
  final int? serviceRequestId;

  OfferSearchRequest({super.page, super.pageSize, this.status, this.serviceRequestId});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (status != null) 'status': status.toString(),
    if (serviceRequestId != null) 'serviceRequestId': serviceRequestId.toString(),
  };
}
