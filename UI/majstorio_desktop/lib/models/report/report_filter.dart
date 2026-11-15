// The filters of both reports. A filter that is not set is left out of the request.
class ReportFilter {
  // The first and the last day of the period, as picked by the administrator.
  final DateTime? fromDate;
  final DateTime? toDate;
  final int? cityId;
  // Only the service requests report uses the category and the status.
  final int? serviceCategoryId;
  final int? status;

  ReportFilter({
    this.fromDate,
    this.toDate,
    this.cityId,
    this.serviceCategoryId,
    this.status,
  });

  // The values that go into the URL after the question mark.
  Map<String, String> toQueryParameters() => {
    if (fromDate != null) 'fromUtc': fromDate!.toUtc().toIso8601String(),
    // The API takes the end as "before this moment", so the whole last day is included.
    if (toDate != null)
      'toUtc': toDate!.add(const Duration(days: 1)).toUtc().toIso8601String(),
    if (cityId != null) 'cityId': cityId.toString(),
    if (serviceCategoryId != null)
      'serviceCategoryId': serviceCategoryId.toString(),
    if (status != null) 'status': status.toString(),
  };
}
