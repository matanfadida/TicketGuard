# TicketGuard

## 1. Goal

An online system for booking tickets to shows and events.

**The challenge:** when sales open, a large number of people arrive at the same moment and try to buy the same seats. Letting everyone choose seats at once is not possible, so buyers wait in a queue before they can start choosing seats.

## 2. Users

| User | Description | Can do |
| --- | --- | --- |
| Admin | Manages events | Create, edit and cancel events, set sale rules, view sales |
| Guest customer | Identified by email and phone, no account | Buy tickets; tickets are sent by email and not kept in the system for the guest |
| Registered customer | Has an account | Buy tickets with saved details filled in automatically, optional saved card (payment provider token), view orders and tickets |

## 3. Flows

### Buying tickets: main flow

1. The customer opens the site and picks an event.
2. If the sale is open and busy, the customer waits in the queue on a waiting page.
3. When their turn comes, they enter seat selection and choose seats.
4. They continue to the order summary.
5. They pay.
6. The order is complete and the tickets are sent.

### Waiting room (queue)

- The waiting page opens shortly before the sale starts (for example, 30 minutes).
- Everyone who arrives before the sale starts gets a random place in the queue, so arriving first gives no advantage.
- Customers who arrive after the sale starts join the end of the queue in arrival order.
- Every buyer always goes through the queue, even when it is empty. If fewer than **X** customers are in seat selection, the buyer is admitted immediately and never sees the waiting page. This keeps one code path, gives guests an identity for holding seats, and leaves no way to skip the queue.
- Up to **X** customers can be in seat selection at the same time.
- When a customer moves on to payment, the next customer in the queue enters seat selection.
- A customer who closes the tab while waiting can come back to the same place from the same device: their queue ID is kept locally. The ID is a token signed by the server, so it cannot be edited to jump ahead.

### Time limits

- When customers are waiting in the queue, a customer has **Y** minutes to choose seats and get to payment.
- When nobody is waiting, a customer gets a longer time (**Y2**, for example 30 minutes). Time is still limited, so seats held by someone who left are released.
- If a queue forms while a customer is inside, their remaining time is set to exactly Y minutes from that moment: shortened if they had more, extended if they had less. A popup tells them they now have Y minutes.
- If time runs out, the customer goes back to the queue.
- On reaching payment, the time is extended by **K** minutes. Time is never unlimited, so a customer cannot hold seats forever by sitting on a screen.
- Once a payment request is sent, the clock stops until the provider answers.
- If the payment is declined, the customer gets a few more minutes to try again, and the seats stay held.
- If the provider does not answer within a maximum time, the system checks the payment status with the provider before releasing the seats.

### Choosing a seat at the same moment

- Seats are taken with an atomic update in the database: the update succeeds only if the seat is still free.
- The first customer gets the seat; the second gets an immediate "seat taken" response.
- Chosen over a message queue because customers need an immediate answer.

### Bots

- Bot protection at the edge with Cloudflare, or an equivalent.

## 4. Business rules

- Each seat is sold once.
- No sales for an event that has already started, or that starts within 10 minutes.
- Each customer can buy up to **Z** tickets per event, or the event can have no limit.
- A guest is identified by email and a phone number verified with an SMS code, to make it harder to get around the ticket limit.
- The admin sets X, Y, Y2, K and Z for each event.
- Saved cards are stored only as a token from the payment provider, never as card numbers (PCI).

## 5. Entities

### Venue
A fixed venue, created once and reused for many events.
Name, city, address, postal code, stage position. The number of seats is not stored; it is counted from the venue's seats.

### Section
An area inside a venue (for example: Orchestra, Balcony, VIP). Belongs to a venue.

### Seat
A fixed seat, created once with the venue. Belongs to a section.
Row, number, and position on the seat map (X, Y).

### Event
Name, artist, venue, start date and time, sale start time, waiting room opening time, status (draft, published, cancelled), created and updated dates.
Sale settings set by the admin: X (customers in seat selection at once), Y (minutes to choose seats when there is a queue), Y2 (minutes when there is no queue), K (extra minutes at payment), Z (tickets per customer, empty means no limit).
Optional: description and image.

### Event section price
The price of a section in a specific event. Links an event and a section.

### Event seat
The link between an event and a seat: the state of one seat in one event.
Status (available, held, sold), the queue entry holding it, and until when.
The atomic "take this seat only if it is still available" update runs on this row.
Choosing a seat holds it on this row right away; no order is needed for that.

### User
A registered user. Guests are not stored as users; an order made by a guest has no user (nullable link).
First name, last name, email, phone, password hash, role (Customer or Admin), created and updated dates.
One saved card per user, replaced if the user adds a new one: payment provider token, last 4 digits and expiry (for display only, never the card number).

### Order
Event, user (nullable, empty for guests), contact email and phone, total price, status, created and updated dates.
Contact email and phone are stored on every order, for guests and registered users alike. Tickets are sent to this email, and the event's ticket limit (Z) is checked by email and phone across orders for that event.

The total price is stored when the order is placed and does not change if section prices change later.
Status: Pending (seats held, waiting for payment), Paid, Failed, Expired (time ran out), Cancelled.

### Ticket
One seat in one order. An order has many tickets; each ticket belongs to one order and points to one event seat.
Unique entry code, and the price paid for this seat, stored at purchase time.
Tickets are created only after a successful payment, so each event seat has at most one ticket (enforced with a unique constraint).

### Queue entry
The buyer's session in one event's sale, for guests and registered users alike. Held seats point to it.
Event, position in the queue, status (Waiting, Choosing seats, Paying, Expired, Done), and the deadline for the current step.
Identified by a token signed by the server, kept on the buyer's device.

### Lifecycle of a purchase

1. Choosing a seat: the event seat becomes Held by the queue entry, until its deadline.
2. Moving to payment: a Pending order is created.
3. Payment succeeds: tickets are created and the event seats become Sold.
4. Time runs out: the held seats return to Available. No tickets were created, so nothing needs cleaning up.

### Payment
One attempt to pay for an order. An order can have several failed attempts but at most one successful payment.
Order, amount, payment method (enum), status, error (if any), created date, and a link to the invoice.
Transaction ID from the payment provider, used to check a payment's real status and for refunds.
Idempotency key: a unique key per payment attempt, with a unique constraint. A repeated request with the same key never charges again: it returns the earlier result, or checks the status with the provider if the earlier result is unknown.
Status: Sent (waiting for the provider), Succeeded, Declined, Error or no answer (triggers a status check with the provider).
A unique constraint allows only one Succeeded payment per order.

### Invoice
Issued by an external invoicing service (simulated). Always a tax invoice-receipt, one per successful payment.
Invoice ID from the invoicing service, and status: Pending, Issued, Failed.
If issuing fails, the order stays paid and the invoice is retried in the background.

## 6. API

### Public

| Action | Access |
| --- | --- |
| Get events | Anyone |
| Get event | Anyone |
| Get event seats (seat map with status and prices) | Anyone |

### Account

| Action | Access |
| --- | --- |
| Register | Anyone |
| Login | Anyone |
| Send SMS code / verify SMS code | Anyone |
| Get user details | The user |
| Update user details | The user |

### Buying

| Action | Access |
| --- | --- |
| Join queue (returns a signed queue token) | Anyone |
| Get queue status and position | Queue token holder |
| Hold seat | Queue token holder, while choosing seats |
| Release seat | Queue token holder |
| Create order | Queue token holder |
| Create payment (with idempotency key) | Queue token holder |

### My orders

| Action | Access |
| --- | --- |
| Get orders | The user (own orders only) |
| Get tickets | The user (own tickets only) |
| Get invoice | The user (own invoices only) |
| Get payments | The user (own payments only) |

### Admin

| Action | Access |
| --- | --- |
| Create and update events | Admin |
| Cancel event | Admin |
| Create and update venues, sections and seats | Admin |
| Set section prices for an event | Admin |
| Sales report for an event | Admin |

## 7. Non-functional requirements

### Load
- Designed as a large system: 50,000 buyers arriving when a sale opens.
- The queue protects seat selection: only X buyers are inside at once (for example, 10,000), so seat selection stays fast no matter how many are waiting.
- The load moves to the queue: every waiting buyer checks their position every few seconds, so the queue status check must be very cheap.

### Speed
- Holding a seat: 95% of requests under 200 ms.

### Availability
- Several API servers behind a load balancer; if one fails, the others keep serving.
- Servers are stateless: nothing important lives in one server's memory.
- The queue, the count of buyers inside, and the cached seat map live in Redis, shared by all servers.
- Seats, orders, tickets and payments live in SQL Server, the source of truth.
- If all servers go down, the sale pauses but nothing is lost: queue positions are in Redis, seats and orders in SQL Server, every hold has a deadline, and payments that were in progress are checked with the provider when the servers come back.

### Hosting
- Everything runs in Docker containers. The cloud provider (AWS or Azure) is decided at the deployment phase.

### Logging
- Serilog with structured logging: values such as seat, order and queue IDs are stored as searchable fields.
- A correlation ID follows each request across all steps and servers.

### Tests
- 100 parallel requests for the same seat: exactly one succeeds.
- The same idempotency key twice: one charge.
- Time runs out: held seats are released.
- The ticket limit (Z) is enforced.
- A user cannot see another user's order.
- Load test to measure the numbers above.

## 8. Scope

### In scope
- Customer website (Angular): queue, seat map, checkout and payment. Responsive, works on mobile.
- Admin website (Angular): venues, events, prices and sales.
- Real external services wherever a free tier or sandbox exists (payments, SMS, email), each behind an interface with a fake implementation for local development and tests.

### Out of scope
- Cancellations and refunds: a purchased ticket cannot be cancelled, and an admin can cancel an event only while no tickets have been sold for it.
- Native mobile app: the responsive website is enough.

## Roadmap

- [ ] **1. Setup:** solution structure, Docker Compose with SQL Server and Redis, CI on GitHub Actions
- [ ] **2. Venues, events and seats:** seat holds with an atomic update, and a test of 100 parallel requests for one seat
- [ ] **3. Queue in Redis**
- [ ] **4. Orders and payments:** idempotency and status checks with the provider
- [ ] **5. Users, JWT, SMS and email**
- [ ] **6. Angular frontend:** customer and admin websites
- [ ] **7. Cloud deployment and load test**
