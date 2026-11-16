import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the appointment list. Mirrors AppointmentSearchRequest on the backend.
class AppointmentSearchRequest extends BaseSearchRequest {
  // One of the numbers in Appointment.statusNames; null shows every status.
  final int? status;

  AppointmentSearchRequest({super.page, super.pageSize, this.status});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (status != null) 'status': status.toString(),
  };
}
