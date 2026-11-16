import 'package:majstorio_desktop/models/appointment/appointment.dart';
import 'package:majstorio_desktop/models/offer/offer.dart';

// One row of the request list. Mirrors ServiceRequestResponse on the backend.
class ServiceRequest {
  // The status numbers the API uses for a request.
  static const int completed = 3;
  static const int cancelled = 4;
  static const Map<int, String> statusNames = {
    0: 'Created',
    1: 'Offer accepted',
    2: 'Appointment confirmed',
    3: 'Completed',
    4: 'Cancelled',
  };

  final int id;
  final String customerName;
  final String cityName;
  final String serviceCategoryName;
  final String description;
  final String? address;
  final DateTime desiredStartUtc;
  final double maximumBudget;
  final int status;
  final DateTime createdAtUtc;

  ServiceRequest({
    required this.id,
    required this.customerName,
    required this.cityName,
    required this.serviceCategoryName,
    required this.description,
    this.address,
    required this.desiredStartUtc,
    required this.maximumBudget,
    required this.status,
    required this.createdAtUtc,
  });

  String get statusName => statusNames[status] ?? 'Unknown';

  // A request can be cancelled until it is completed (or already cancelled).
  bool get canBeCancelled => status != completed && status != cancelled;

  factory ServiceRequest.fromJson(Map<String, dynamic> json) {
    return ServiceRequest(
      id: json['id'],
      customerName: json['customerName'],
      cityName: json['cityName'],
      serviceCategoryName: json['serviceCategoryName'],
      description: json['description'],
      address: json['address'],
      desiredStartUtc: DateTime.parse(json['desiredStartUtc']),
      maximumBudget: (json['maximumBudget'] as num).toDouble(),
      status: json['status'],
      createdAtUtc: DateTime.parse(json['createdAtUtc']),
    );
  }
}

// One recorded status change: who, when, from what and to what.
class StatusHistoryEntry {
  final String changedByName;
  final DateTime changedAtUtc;
  // Null on the first entry, when the request was created.
  final int? previousStatus;
  final int newStatus;
  final String? note;

  StatusHistoryEntry({
    required this.changedByName,
    required this.changedAtUtc,
    this.previousStatus,
    required this.newStatus,
    this.note,
  });

  factory StatusHistoryEntry.fromJson(Map<String, dynamic> json) {
    return StatusHistoryEntry(
      changedByName: json['changedByName'],
      changedAtUtc: DateTime.parse(json['changedAtUtc']),
      previousStatus: json['previousStatus'],
      newStatus: json['newStatus'],
      note: json['note'],
    );
  }
}

// Everything the detail screen shows. Mirrors ServiceRequestDetailResponse on the backend.
class ServiceRequestDetail {
  final ServiceRequest request;
  final List<Offer> offers;
  // Null until an offer is accepted.
  final Appointment? appointment;
  final List<StatusHistoryEntry> statusHistory;

  ServiceRequestDetail({
    required this.request,
    required this.offers,
    this.appointment,
    required this.statusHistory,
  });

  factory ServiceRequestDetail.fromJson(Map<String, dynamic> json) {
    return ServiceRequestDetail(
      request: ServiceRequest.fromJson(json),
      offers: (json['offers'] as List).map((offer) => Offer.fromJson(offer)).toList(),
      appointment: json['appointment'] == null ? null : Appointment.fromJson(json['appointment']),
      statusHistory: (json['statusHistory'] as List)
          .map((entry) => StatusHistoryEntry.fromJson(entry))
          .toList(),
    );
  }
}
