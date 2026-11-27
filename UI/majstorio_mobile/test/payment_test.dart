import 'dart:convert';

import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/providers/payment/payment_provider.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';

import 'screens_test.dart' show app, signInAs;
import 'test_data.dart';

Map<String, dynamic> completedJob({bool isPaid = false}) => requestDetailJson(
  status: ServiceRequest.completed,
  offers: [offerJson(id: 1, status: 1, price: 120)],
  appointment: {...appointmentJson(status: Appointment.completed), 'isPaid': isPaid},
);

// Shows the request detail on a tall screen, so the payment section is built without scrolling.
Future<FakeApi> openJob(WidgetTester tester, Map<String, dynamic> detail) async {
  tester.view.physicalSize = const Size(1080, 6000);
  tester.view.devicePixelRatio = 1;
  addTearDown(tester.view.reset);
  var api = FakeApi((request) =>
      request.url.path.startsWith('/Reviews') ? jsonResponse(page([])) : jsonResponse(detail));
  await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
  await tester.pumpAndSettle();
  return api;
}

void main() {
  setUp(AuthSession.clear);

  test('start sends only the appointment, never an amount', () async {
    var api = FakeApi((request) => jsonResponse({
      'paymentId': 3,
      'approvalUrl': 'https://www.sandbox.paypal.com/checkoutnow?token=X',
      'returnUrl': 'https://majstorio.app/paypal/return',
      'cancelUrl': 'https://majstorio.app/paypal/cancel',
    }));

    var checkout = await PaymentProvider().start(7);

    expect(api.calls.single.url.path, '/Payments');
    expect(jsonDecode(api.calls.single.body), {'appointmentId': 7});
    expect(checkout.paymentId, 3);
  });

  testWidgets('customer of an unpaid completed job sees the pay button', (tester) async {
    signInAs(AuthSession.customer);
    await openJob(tester, completedJob());

    expect(find.text('Plati putem PayPala'), findsOneWidget);
  });

  testWidgets('a paid job shows "Plaćeno" and no pay button', (tester) async {
    signInAs(AuthSession.customer);
    await openJob(tester, completedJob(isPaid: true));

    expect(find.text('Plaćeno'), findsOneWidget);
    expect(find.text('Plati putem PayPala'), findsNothing);
  });

  testWidgets('pay asks first with the agreed price and calls the API only after "Da"', (tester) async {
    signInAs(AuthSession.customer);
    var api = await openJob(tester, completedJob());

    await tester.tap(find.text('Plati putem PayPala'));
    await tester.pumpAndSettle();
    expect(find.text('Platiti 120,00 € putem PayPala?'), findsOneWidget);
    await tester.tap(find.text('Ne'));
    await tester.pumpAndSettle();

    expect(api.calls.where((call) => call.url.path.startsWith('/Payments')), isEmpty);
  });
}
