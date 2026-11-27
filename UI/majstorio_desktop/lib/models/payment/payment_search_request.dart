import 'package:majstorio_desktop/models/base_search_request.dart';

// Filters for the payment list. Mirrors PaymentSearchRequest on the backend.
class PaymentSearchRequest extends BaseSearchRequest {
  // One of the numbers in Payment.statusNames; null shows every status.
  final int? status;

  PaymentSearchRequest({super.page, super.pageSize, this.status});

  @override
  Map<String, String> toQueryParameters() => {
    ...super.toQueryParameters(),
    if (status != null) 'status': status.toString(),
  };
}
