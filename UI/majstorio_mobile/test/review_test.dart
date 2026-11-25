import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/screens/review/review_form.dart';
import 'package:majstorio_mobile/screens/service_request/service_request_details_screen.dart';

import 'screens_test.dart' show app, signInAs;
import 'test_data.dart';

Map<String, dynamic> completedDetail() => requestDetailJson(
  status: ServiceRequest.completed,
  offers: [offerJson(id: 1, status: 1)],
  appointment: appointmentJson(status: Appointment.completed, start: DateTime.utc(2026, 11, 1, 10)),
);

Map<String, dynamic> reviewJson() => {
  'id': 3,
  'appointmentId': 7,
  'tradespersonName': 'Majstor 1',
  'rating': 4,
  'comment': 'Uredno i brzo.',
  'createdAtUtc': '2026-11-02T10:00:00',
};

ReviewForm form() => ReviewForm(appointment: Appointment.fromJson(appointmentJson(status: Appointment.completed)));

Future<void> chooseFiveAndSubmit(WidgetTester tester) async {
  await tester.tap(find.byType(DropdownButtonFormField<int>));
  await tester.pumpAndSettle();
  await tester.tap(find.text('5 - Odlično').last);
  await tester.pumpAndSettle();
  await tester.tap(find.text('Pošalji ocjenu'));
  await tester.pumpAndSettle();
}

void main() {
  setUp(AuthSession.clear);

  testWidgets('a completed job without a rating offers the button', (tester) async {
    signInAs(AuthSession.customer);
    FakeApi((request) => jsonResponse(request.url.path == '/Reviews/mine' ? page([]) : completedDetail()));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(find.text('Ocijeni majstora'), 300, scrollable: find.byType(Scrollable).first);
    expect(find.text('Ocijeni majstora'), findsOneWidget);
  });

  testWidgets('a rated job shows the rating instead of the button', (tester) async {
    signInAs(AuthSession.customer);
    FakeApi((request) => jsonResponse(request.url.path == '/Reviews/mine' ? page([reviewJson()]) : completedDetail()));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(find.text('4/5'), 300, scrollable: find.byType(Scrollable).first);
    expect(find.text('Ocijeni majstora'), findsNothing);
  });

  testWidgets('without a grade nothing is sent', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) => http.Response('', 200));

    await tester.pumpWidget(app(form()));
    await tester.tap(find.text('Pošalji ocjenu'));
    await tester.pumpAndSettle();

    expect(find.text('Odaberite ocjenu'), findsOneWidget);
    expect(api.calls, isEmpty);
  });

  testWidgets('"Ne" in the dialog sends nothing, "Da" sends the rating', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) => http.Response('', 200));

    await tester.pumpWidget(app(form()));
    await chooseFiveAndSubmit(tester);
    await tester.tap(find.text('Ne'));
    await tester.pumpAndSettle();
    expect(api.calls, isEmpty);

    await tester.tap(find.text('Pošalji ocjenu'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Da'));
    await tester.pumpAndSettle();

    expect(api.calls.single.method, 'POST');
    expect(api.calls.single.url.path, '/Reviews/mine');
    expect(api.calls.single.body, '{"rating":5,"appointmentId":7,"comment":null}');
  });

  testWidgets('an already rated job (409) shows the reason and stays on the form', (tester) async {
    signInAs(AuthSession.customer);
    FakeApi((request) => jsonResponse({'message': 'A review already exists.'}, 409));

    await tester.pumpWidget(app(form()));
    await chooseFiveAndSubmit(tester);
    await tester.tap(find.text('Da'));
    await tester.pumpAndSettle();

    expect(find.text('Ovaj posao je već ocijenjen.'), findsOneWidget);
    expect(find.byType(ReviewForm), findsOneWidget);
  });
}
