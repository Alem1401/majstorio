import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_mobile/models/notification/notification.dart';
import 'package:majstorio_mobile/models/notification/notification_search_request.dart';

void main() {
  test('a notification is read from the API names', () {
    var notification = AppNotification.fromJson({
      'id': 5,
      'serviceRequestId': 12,
      'title': 'Nova ponuda',
      'message': 'Stigla je nova ponuda.',
      'isRead': false,
      'createdAtUtc': '2026-11-21T10:00:00',
    });
    expect(notification.id, 5);
    expect(notification.serviceRequestId, 12);
    expect(notification.title, 'Nova ponuda');
    expect(notification.message, 'Stigla je nova ponuda.');
    expect(notification.isRead, isFalse);
    expect(notification.createdAtUtc, DateTime.utc(2026, 11, 21, 10));
  });

  test('a notification without a request keeps the request id empty', () {
    var notification = AppNotification.fromJson({
      'id': 1,
      'serviceRequestId': null,
      'title': 'T',
      'message': 'M',
      'isRead': true,
      'createdAtUtc': '2026-11-21T10:00:00Z',
    });
    expect(notification.serviceRequestId, isNull);
    expect(notification.isRead, isTrue);
  });

  test('the unread filter is sent only when it is set', () {
    expect(NotificationSearchRequest().toQueryParameters(), {'page': '1', 'pageSize': '10'});
    expect(
      NotificationSearchRequest(isRead: false, pageSize: 1).toQueryParameters(),
      {'page': '1', 'pageSize': '1', 'isRead': 'false'},
    );
  });
}
