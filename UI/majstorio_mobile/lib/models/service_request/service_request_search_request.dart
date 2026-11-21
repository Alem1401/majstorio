import 'package:majstorio_mobile/models/base_search_request.dart';
import 'package:majstorio_mobile/utils/formatters.dart';

// Filters for the request list. Mirrors ServiceRequestSearchRequest on the backend.
// The API returns only the requests the signed-in user may see.
class ServiceRequestSearchRequest extends BaseSearchRequest {
  final int? status;
  final int? cityId;
  final int? serviceCategoryId;
  // The period in which the request was created.
  final DateTime? from;
  final DateTime? to;

  ServiceRequestSearchRequest({
    super.page,
    super.pageSize,
    this.status,
    this.cityId,
    this.serviceCategoryId,
    this.from,
    this.to,
  });

  // A filter that is not set is left out.
  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (status != null) 'status': status.toString(),
    if (cityId != null) 'cityId': cityId.toString(),
    if (serviceCategoryId != null) 'serviceCategoryId': serviceCategoryId.toString(),
    if (from != null) 'fromUtc': toApiDate(from!),
    // "To" is a day picked in the calendar; the whole day is included.
    if (to != null) 'toUtc': toApiDate(to!.add(const Duration(days: 1))),
  };
}
