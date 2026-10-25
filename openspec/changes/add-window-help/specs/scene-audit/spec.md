# Spec Delta

## ADDED Requirements

### Requirement: Help in the window
The window SHALL have a Help button in the toolbar whose menu offers "Getting started", help on the open tab, the user
guide and the check reference (pages of the repository opened in the browser). The Problems tab without a result SHALL
show a getting-started screen: the target GPU with a link to the budgets, the scope menu, the run button and task cards
that each take the first step of the task. A group of a check that has help SHALL offer "More about the check": a card
with what is checked, why it matters in VR, how to fix it by hand, the gain (only for checks with a gain estimate) and
when it may stay, with a button that selects the group's objects. Help texts SHALL exist in English and Russian for at
least 30 common checks.

#### Scenario: Getting started
- **WHEN** the Problems tab has no result
- **THEN** it shows the three steps and the task cards; "Before a build" sets the scope to the build scenes and runs the
  audit, and is unavailable when Build Settings has no enabled scenes

#### Scenario: Getting started over a result
- **WHEN** the user picks "Getting started" in the Help menu while a result is shown
- **THEN** the Problems tab shows the getting-started screen with "Back to the Result", and a new run hides it

#### Scenario: More about a check
- **WHEN** a group of "Texture too large" is open and the user clicks "More about the check"
- **THEN** a card shows the sections of the check in the window language, with "Gain", and the link becomes "Hide
  details"

#### Scenario: Help on the tab
- **WHEN** the Lighting tab is open and the user picks "Help on the Lighting tab (in the browser, Russian)"
- **THEN** the guide opens in the browser at its section about the Lighting tab
