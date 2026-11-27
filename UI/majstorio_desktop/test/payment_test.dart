import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_desktop/models/payment/payment.dart';
import 'package:majstorio_desktop/models/payment/payment_search_request.dart';

void main() {
  test('only a paid payment that is not refunded can be refunded, and the captured amount is shown', () {
    Payment payment({int status = 1, bool isRefunded = false}) => Payment.fromJson({
      'id': 1,
      'customerName': 'Mobile Test',
      'tradespersonName': 'Majstor',
      'amount': 120,
      'capturedAmount': 119.5,
      'status': status,
      'paidAtUtc': '2026-11-20T10:00:00Z',
      'isRefunded': isRefunded,
    });

    expect(payment().canBeRefunded, isTrue);
    expect(payment().amount, 119.5);
    expect(payment(isRefunded: true).canBeRefunded, isFalse);
    expect(payment(status: 0).canBeRefunded, isFalse);
  });

  test('search sends the status only when it is chosen', () {
    expect(PaymentSearchRequest(status: 1).toQueryParameters()['status'], '1');
    expect(PaymentSearchRequest().toQueryParameters().containsKey('status'), isFalse);
  });
}
