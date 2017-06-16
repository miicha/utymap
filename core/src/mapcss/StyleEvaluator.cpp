#include "mapcss/StyleEvaluator.hpp"

#include <boost/config/warning_disable.hpp>
#include <boost/spirit/include/qi.hpp>
#include <boost/fusion/include/adapt_struct.hpp>

using namespace utymap::entities;
using namespace utymap::index;
using namespace utymap::mapcss;

namespace {

typedef StyleEvaluator::Nil Nil;
typedef StyleEvaluator::Signed Signed;
typedef StyleEvaluator::Tree Tree;
typedef StyleEvaluator::Operation Operation;
typedef StyleEvaluator::Operand Operand;

namespace qi = boost::spirit::qi;
namespace ascii = boost::spirit::ascii;

/// Grammar for parsing string into AST.
template<typename Iterator>
struct EvalGrammar : qi::grammar<Iterator, Tree(), ascii::space_type> {
  EvalGrammar() : EvalGrammar::base_type(expression) {
    qi::double_type double_;
    qi::char_type char_;

    expression =
      "eval(\"" >> term >> *((char_("+") >> term) | (char_("-") >> term)) >> "\")";

    term =
      factor >> *((char_("*") >> factor) | (char_("/") >> factor));

    tag =
      "tag('" >> qi::lexeme[+(ascii::char_ - '\'')] >> "')";

    factor =
      double_
      | tag
      | '(' >> expression >> ')'
      | (char_("-") >> factor)
      | (char_("+") >> factor);
  }

  qi::rule<Iterator, std::string()> tag;
  qi::rule<Iterator, Tree(), ascii::space_type> expression;
  qi::rule<Iterator, Tree(), ascii::space_type> term;
  qi::rule<Iterator, Operand(), ascii::space_type> factor;
};
}

BOOST_FUSION_ADAPT_STRUCT(
  Signed,
  (char, sign)
  (Operand, operand)
)

BOOST_FUSION_ADAPT_STRUCT(
  Operation,
  (char, operator_)
  (Operand, operand)
)

BOOST_FUSION_ADAPT_STRUCT(
  Tree,
  (Operand, first)
  (std::list<Operation>, rest)
)

std::unique_ptr<Tree> StyleEvaluator::parse(const std::string &expression) {
  auto tree = utymap::utils::make_unique<Tree>();

  EvalGrammar<std::string::const_iterator> grammar;

  std::string::const_iterator iter = expression.begin();
  std::string::const_iterator end = expression.end();
  boost::spirit::ascii::space_type space;
  bool r = phrase_parse(iter, end, grammar, space, *tree);

  if (!r || iter!=end) tree.reset();

  return tree;
}
