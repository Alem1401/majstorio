import 'package:majstorio_mobile/models/base_search_request.dart';

// Filters for GET /Appointments/mine. Mirrors AppointmentSearchRequest on the backend.
class AppointmentSearchRequest extends BaseSearchRequest {
  final int? status;

  AppointmentSearchRequest({super.page, super.pageSize, this.status});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (status != null) 'status': status.toString(),
  };
}
