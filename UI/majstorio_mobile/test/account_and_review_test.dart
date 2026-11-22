import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:majstorio_mobile/models/appointment/appointment.dart';
import 'package:majstorio_mobile/models/auth/auth_session.dart';
import 'package:majstorio_mobile/models/service_request/service_request.dart';
import 'package:majstorio_mobile/providers/account/account_provider.dart';
import 'package:majstorio_mobile/screens/account/profile_edit_screen.dart';
import 'package:majstorio_mobile/screens/review/review_form_screen.dart';
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
  'tradespersonProfileId': 101,
  'customerId': 5,
  'customerName': 'Mobile Test',
  'tradespersonName': 'Majstor 1',
  'rating': 4,
  'comment': 'Uredno i brzo.',
  'createdAtUtc': '2026-11-02T10:00:00',
};

Map<String, dynamic> profileJson() => {
  'id': 5,
  'userName': 'mobile',
  'email': 'mobile@majstorio.test',
  'phoneNumber': null,
  'firstName': 'Amra',
  'lastName': 'Hadžić',
  'profileImageUrl': null,
  'isActive': true,
  'role': 'Customer',
};

void main() {
  setUp(() {
    AuthSession.clear();
  });

  testWidgets('owner of a completed job without a rating can rate it', (tester) async {
    signInAs(AuthSession.customer);
    FakeApi((request) {
      if (request.url.path == '/Reviews/mine') return jsonResponse(page([]));
      return jsonResponse(completedDetail());
    });

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(find.text('Ocijeni majstora'), 300, scrollable: find.byType(Scrollable).first);
    expect(find.text('Ocijeni majstora'), findsOneWidget);
    expect(find.text('Otkaži zahtjev'), findsNothing);
  });

  testWidgets('a rated job shows the rating instead of the button', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) {
      if (request.url.path == '/Reviews/mine') return jsonResponse(page([reviewJson()]));
      return jsonResponse(completedDetail());
    });

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    await tester.scrollUntilVisible(find.text('4/5'), 300, scrollable: find.byType(Scrollable).first);
    expect(find.text('Ocijeni majstora'), findsNothing);
    expect(find.text('4/5'), findsOneWidget);
    expect(find.text('Uredno i brzo.'), findsOneWidget);
    var reviewCall = api.calls.firstWhere((call) => call.url.path == '/Reviews/mine');
    expect(reviewCall.url.queryParameters['appointmentId'], '7');
  });

  testWidgets('a tradesperson sees no rating section on a completed job', (tester) async {
    signInAs(AuthSession.tradesperson, userId: 8);
    var api = FakeApi((request) => jsonResponse(completedDetail()));

    await tester.pumpWidget(app(const ServiceRequestDetailsScreen(requestId: 10)));
    await tester.pumpAndSettle();

    expect(find.text('Ocijeni majstora'), findsNothing);
    expect(api.calls.where((call) => call.url.path == '/Reviews/mine'), isEmpty);
  });

  testWidgets('rating needs a star, then sends rating and comment after confirmation', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) => http.Response('', 200));
    var appointment = Appointment.fromJson(appointmentJson(status: Appointment.completed));

    await tester.pumpWidget(app(ReviewFormScreen(appointment: appointment)));
    await tester.tap(find.text('Pošalji ocjenu'));
    await tester.pump();
    expect(find.text('Odaberite ocjenu od 1 do 5'), findsOneWidget);
    expect(api.calls, isEmpty);

    await tester.tap(find.byTooltip('5 od 5'));
    await tester.enterText(find.byType(TextField), 'Odlično');
    await tester.tap(find.text('Pošalji ocjenu'));
    await tester.pumpAndSettle();
    await tester.tap(find.text('Da'));
    await tester.pumpAndSettle();

    var post = api.calls.single;
    expect(post.url.path, '/Reviews/mine');
    expect(post.body, '{"appointmentId":7,"rating":5,"comment":"Odlično"}');
  });

  testWidgets('profile edit loads the account and saves the changed details', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) {
      if (request.method == 'PUT') return http.Response('', 200);
      return jsonResponse(profileJson());
    });

    await tester.pumpWidget(app(const ProfileEditScreen()));
    await tester.pumpAndSettle();
    expect(find.text('Amra'), findsOneWidget);

    await tester.enterText(find.widgetWithText(TextFormField, 'Telefon (nije obavezno)'), '+387 61 123 456');
    await tester.ensureVisible(find.text('Spremi'));
    await tester.tap(find.text('Spremi'));
    await tester.pumpAndSettle();

    var put = api.calls.singleWhere((call) => call.method == 'PUT');
    expect(put.url.path, '/Account');
    expect(put.body, contains('"phoneNumber":"+387 61 123 456"'));
    expect(put.body, contains('"email":"mobile@majstorio.test"'));
  });

  testWidgets('profile edit refuses an invalid e-mail without calling the API', (tester) async {
    signInAs(AuthSession.customer);
    var api = FakeApi((request) => jsonResponse(profileJson()));

    await tester.pumpWidget(app(const ProfileEditScreen()));
    await tester.pumpAndSettle();
    await tester.enterText(find.widgetWithText(TextFormField, 'E-pošta'), 'nije-email');
    await tester.ensureVisible(find.text('Spremi'));
    await tester.tap(find.text('Spremi'));
    await tester.pumpAndSettle();

    expect(find.text('E-pošta nije ispravna'), findsOneWidget);
    expect(api.calls.where((call) => call.method == 'PUT'), isEmpty);
  });

  test('photo upload is a multipart POST with the token', () async {
    AuthSession.token = 'abc';
    http.BaseRequest? sent;
    FakeApi((request) {
      sent = request;
      return http.Response('', 200);
    });

    await AccountProvider().uploadPhoto([0x89, 0x50, 0x4E, 0x47], 'slika.png');

    expect(sent!.method, 'POST');
    expect(sent!.url.path, '/Account/photo');
    expect(sent!.headers['Authorization'], 'Bearer abc');
    expect(sent!.headers['content-type'], startsWith('multipart/form-data'));
  });
}
