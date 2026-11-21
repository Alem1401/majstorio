import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/utils/formatters.dart';

import 'test_data.dart';

void main() {
  test('a time without a zone is read as UTC', () {
    var value = parseUtc('2026-11-21T10:00:00');
    expect(value.isUtc, isTrue);
    expect(value.hour, 10);
    expect(parseUtc('2026-11-21T10:00:00Z'), value);
  });

  test('request detail keeps the server order of the history and finds the accepted offer', () {
    var detail = ServiceRequestDetail.fromJson(
      requestDetailJson(
        status: ServiceRequest.appointmentConfirmed,
        offers: [offerJson(id: 1, status: 1, price: 80), offerJson(id: 2, status: 4)],
        appointment: appointmentJson(),
        history: [historyJson(id: 3, newStatus: 2), historyJson(id: 1, newStatus: 0), historyJson(id: 2, newStatus: 1)],
      ),
    );
    expect(detail.statusHistory.map((entry) => entry.id), [1, 2, 3]);
    expect(detail.acceptedOffer?.price, 80);
    expect(detail.pendingOffers, isEmpty);
    expect(detail.appointment?.statusName, 'Potvrđen');
  });

  test('an unknown status gets a neutral label and no action', () {
    var request = ServiceRequest.fromJson(requestJson(status: 9));
    expect(request.statusName, 'Nepoznato');
    expect(request.canBeCancelled, isFalse);
  });

  test('finished requests cannot be cancelled', () {
    expect(ServiceRequest.fromJson(requestJson(status: ServiceRequest.completed)).canBeCancelled, isFalse);
    expect(ServiceRequest.fromJson(requestJson(status: ServiceRequest.cancelled)).canBeCancelled, isFalse);
    expect(ServiceRequest.fromJson(requestJson(status: ServiceRequest.created)).canBeCancelled, isTrue);
  });

  test('an appointment can be completed only when confirmed and started', () {
    var now = DateTime.utc(2026, 11, 21, 12);
    Appointment at(int status, DateTime start) =>
        Appointment.fromJson(appointmentJson(status: status, start: start));

    expect(at(Appointment.confirmed, now.subtract(const Duration(hours: 1))).canBeCompleted(now), isTrue);
    expect(at(Appointment.confirmed, now.add(const Duration(hours: 1))).canBeCompleted(now), isFalse);
    expect(at(Appointment.pending, now.subtract(const Duration(hours: 1))).canBeCompleted(now), isFalse);
    expect(at(Appointment.completed, now.subtract(const Duration(hours: 1))).canBeCompleted(now), isFalse);
  });

  test('money and durations are shown in the app format', () {
    expect(formatMoney(120.5), '120,50 €');
    expect(formatDuration(90), '1 h 30 min');
    expect(formatDuration(45), '45 min');
    expect(toApiDate(DateTime.utc(2026, 11, 21, 10)), '2026-11-21T10:00:00.000Z');
  });
}
